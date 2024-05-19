using Silk.NET.Vulkan;
using StbImageSharp;
using System.Reflection.Metadata;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal class VImage : IDisposable
    {

        private static byte[] LoadImage(string imageFile, out uint width, out uint height)
        {
            ImageResult result = ImageResult.FromStream(File.OpenRead(imageFile), ColorComponents.RedGreenBlueAlpha);
            width = (uint)result.Width;
            height = (uint)result.Height;
            return result.Data;
        }

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly uint _width;
        private readonly uint _height;

        private readonly ImageAspectFlags _aspectMask;

        private readonly Image _image;
        private readonly DeviceMemory _imageMemory;

        private Sampler _sampler;
        private ImageLayout _layout = ImageLayout.Undefined;

        public uint Width => _width;

        public uint Height => _height;

        public Format Format { get; }

        public ImageView ImageView { get; }

        public Sampler Sampler => _sampler.Handle != 0 ? _sampler : throw new NullReferenceException("Sampler not initialised yet!");

        public VImage(string imageFile, Format format, VulkanEngine engine, Vk vk, ImageAspectFlags aspectMask = ImageAspectFlags.ColorBit)
        {
            _engine = engine;
            _vk = vk;
            _aspectMask = CompleteAspectMask(aspectMask, format);
            Format = format;
            byte[] imageData = LoadImage(imageFile, out _width, out _height);
            var stagingBuffer = new VBuffer<byte>(imageData, BufferUsageFlags.TransferSrcBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, _engine, _vk);
            _image = CreateImage(ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, out _imageMemory);
            UploadToImage(stagingBuffer);
            TransitionImageLayout(ImageLayout.ShaderReadOnlyOptimal);
            stagingBuffer.Dispose();
            ImageView = CreateImageView();
        }

        public VImage(uint width, uint height, Format format, ImageUsageFlags usage, VulkanEngine engine, Vk vk, ImageAspectFlags aspectMask = ImageAspectFlags.DepthBit)
        {
            _engine = engine;
            _vk = vk;
            _width = width;
            _height = height;
            _aspectMask = CompleteAspectMask(aspectMask, format);
            Format = format;
            _image = CreateImage(usage, out _imageMemory);
            ImageView = CreateImageView();
        }

        public VImage(string imageFile, VulkanEngine engine, Vk vk)
            : this(imageFile, Format.R8G8B8A8Srgb, engine, vk) { }

        private static ImageAspectFlags CompleteAspectMask(ImageAspectFlags aspectMask, Format format)
        {
            if (VkUtils.HasStencilComponent(format))
            {
                return aspectMask | ImageAspectFlags.StencilBit;
            }
            return aspectMask;
        }

        private unsafe Image CreateImage(ImageUsageFlags usage, out DeviceMemory imageMemory)
        {
            var imageInfo = new ImageCreateInfo(
                imageType: ImageType.Type2D,
                extent: new Extent3D(
                    width: _width,
                    height: _height,
                    depth: 1
                ),
                mipLevels: 1,
                arrayLayers: 1,

                format: Format,

                tiling: ImageTiling.Optimal,

                initialLayout: ImageLayout.Undefined,

                usage: usage,

                sharingMode: SharingMode.Exclusive,

                samples: SampleCountFlags.Count1Bit,
                flags: 0
            );

            if (_vk.CreateImage(_engine.Device, imageInfo, null, out Image image) != Result.Success)
            {
                throw new VulkanException("Failed to create Image!");
            }

            MemoryRequirements memRequirements = _vk.GetImageMemoryRequirements(_engine.Device, image);
            var allocInfo = new MemoryAllocateInfo(
                allocationSize: memRequirements.Size,
                memoryTypeIndex: VkUtils.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit, _engine.PhysicalDeviceMemoryProperties)
            );
            if (_vk.AllocateMemory(_engine.Device, allocInfo, null, out imageMemory) != Result.Success)
            {
                throw new VulkanException("Failed to allocate Image-Memory!");
            }

            VkUtils.AssertVk(_vk.BindImageMemory(_engine.Device, image, imageMemory, 0));

            return image;
        }

        private unsafe ImageView CreateImageView()
        {
            var viewInfo = new ImageViewCreateInfo(
                image: _image,
                viewType: ImageViewType.Type2D,
                format: Format,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: _aspectMask,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: 1
                )
            );
            if (_vk.CreateImageView(_engine.Device, viewInfo, null, out ImageView imageView) != Result.Success)
            {
                throw new VulkanException("Failed to create ImageView!");
            }
            return imageView;
        }

        public unsafe void CreateSampler(Filter filter)
        {
            var samplerInfo = new SamplerCreateInfo(
                magFilter: filter,
                minFilter: filter,

                addressModeU: SamplerAddressMode.Repeat,
                addressModeV: SamplerAddressMode.Repeat,
                addressModeW: SamplerAddressMode.Repeat,

                anisotropyEnable: true,
                maxAnisotropy: _engine.PhysicalDeviceProperties.Limits.MaxSamplerAnisotropy,

                borderColor: BorderColor.IntOpaqueBlack,

                unnormalizedCoordinates: false,

                compareEnable: false,
                compareOp: CompareOp.Always,

                mipmapMode: SamplerMipmapMode.Linear,
                mipLodBias: 0.0f,
                minLod: 0.0f,
                maxLod: 0.0f
            );
            if (_vk.CreateSampler(_engine.Device, samplerInfo, null, out _sampler) != Result.Success)
            {
                throw new VulkanException("Failed to create Image-Sampler!");
            }
        }

        public unsafe void UploadToImage(VBuffer<byte> stagingBuffer)
        {
            TransitionImageLayout(ImageLayout.TransferDstOptimal);

            CommandBuffer commandBuffer = _engine.BeginSingleUseCommandBuffer();

            var region = new BufferImageCopy(
                bufferOffset: 0,
                bufferRowLength: 0,
                bufferImageHeight: 0,

                imageSubresource: new ImageSubresourceLayers(
                    aspectMask: _aspectMask,
                    mipLevel: 0,
                    baseArrayLayer: 0,
                    layerCount: 1
                ),

                imageOffset: new Offset3D(0, 0, 0),
                imageExtent: new Extent3D(
                    width: Width,
                    height: Height,
                    depth: 1
                )
            );

            _vk.CmdCopyBufferToImage(commandBuffer, stagingBuffer.Handle, _image, ImageLayout.TransferDstOptimal, 1, region);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);
        }

        public unsafe void TransitionImageLayout(ImageLayout newLayout)
        {
            if (_layout == newLayout)
            {
                return;
            }

            CommandBuffer commandBuffer = _engine.BeginSingleUseCommandBuffer();

            var barrier = new ImageMemoryBarrier(
                oldLayout: _layout,
                newLayout: newLayout,

                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

                image: _image,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: _aspectMask,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: 1
                )
            );

            PipelineStageFlags sourceStage = 0;
            PipelineStageFlags destinationStage = 0;

            if (_layout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;

                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.TransferBit;
            }
            else if (_layout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                sourceStage = PipelineStageFlags.TransferBit;
                destinationStage = PipelineStageFlags.FragmentShaderBit;
            }
            else if (_layout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;

                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
            }

            _vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);
            _layout = newLayout;
        }

        public DescriptorImageInfo ImageInfo()
        {
            return new DescriptorImageInfo(
                imageLayout: _layout,
                imageView: ImageView,
                sampler: Sampler
            );
        }

        public unsafe void Dispose()
        {
            if (_sampler.Handle != 0)
            {
                _vk.DestroySampler(_engine.Device, Sampler, null);
            }
            _vk.DestroyImageView(_engine.Device, ImageView, null);

            _vk.FreeMemory(_engine.Device, _imageMemory, null);
            _vk.DestroyImage(_engine.Device, _image, null);
        }

    }
}
