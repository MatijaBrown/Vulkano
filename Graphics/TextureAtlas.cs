using Silk.NET.Vulkan;
using StbImageSharp;
using System.Collections;
using System.Globalization;
using System.Resources;
using Vulkano.Engine;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Graphics
{
    internal class TextureAtlas : IDisposable
    {

        private readonly IDictionary<string, uint> _textureArrayIndices = new Dictionary<string, uint>();

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly Image _image;
        private readonly DeviceMemory _imageMemory;

        private readonly uint _mipLevels;

        private ImageLayout _layout = ImageLayout.Undefined;

        public uint TextureSize { get; }

        public uint SingleTextureByteSize => TextureSize * TextureSize * 4; // RGBA

        public uint AtlasSize { get; }

        public ImageView ImageView { get; }

        public Sampler Sampler { get; }

        public TextureAtlas(Type resourceType, uint atlasSize, Filter filter, VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;
            AtlasSize = atlasSize;

            ImageResult[] images = LoadImages(resourceType, out uint imageSize);
            if (images.Length > AtlasSize)
            {
                throw new ApplicationException("Atlas too small!");
            }
            TextureSize = imageSize;
            _mipLevels = (uint)Math.Floor(Math.Log2(TextureSize)) + 1;
            _image = CreateImage(out _imageMemory);
            VBuffer<byte> stagingBuffer = CreateStagingBuffer(images, out BufferImageCopy[] imageCopies);
            UploadToImage(stagingBuffer, imageCopies);
            stagingBuffer.Dispose();
            GenerateMipmaps();
            ImageView = CreateImageView();
            Sampler = CreateSampler(filter);
        }

        private ImageResult[] LoadImages(Type resourceType, out uint size)
        {
            uint? imageSize = null;
            var manager = new ResourceManager(resourceType);
            ResourceSet? resourceSet = manager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            if (resourceSet == null)
            {
                throw new ApplicationException("Failed to find resources for " + resourceType.Name);
            }
            var imageData = new List<ImageResult>();
            uint textureArrayIndex = 0;
            foreach (DictionaryEntry entry in resourceSet)
            {
                ImageResult result = ImageResult.FromMemory((byte[])entry.Value!, ColorComponents.RedGreenBlueAlpha);
                if (result.Width != result.Height)
                {
                    throw new ApplicationException("Images must be square! \"" + (string)entry.Key + "\" is not!");
                }
                imageSize ??= (uint)result.Width;
                if (imageSize != (uint)result.Width)
                {
                    throw new ApplicationException("Only one image size per TextureAtlas for you!");
                }
                imageData.Add(result);
                _textureArrayIndices.Add((string)entry.Key, textureArrayIndex++);
            }
            resourceSet.Dispose();
            size = imageSize!.Value;
            return imageData.ToArray();
        }

        private unsafe Image CreateImage(out DeviceMemory imageMemory)
        {
            var imageInfo = new ImageCreateInfo(
                imageType: ImageType.Type2D,
                extent: new Extent3D(
                    width: TextureSize,
                    height: TextureSize,
                    depth: 1
                ),
                mipLevels: _mipLevels,
                arrayLayers: AtlasSize,

                format: Format.R8G8B8A8Srgb,

                tiling: ImageTiling.Optimal,

                initialLayout: ImageLayout.Undefined,

                usage: ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,

                sharingMode: SharingMode.Exclusive,

                samples: SampleCountFlags.Count1Bit
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

        private VBuffer<byte> CreateStagingBuffer(ImageResult[] results, out BufferImageCopy[] imageCopies)
        {
            var buffer = new VBuffer<byte>((uint)(results.Length * SingleTextureByteSize),
                BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, _engine, _vk);
            buffer.Map();
            imageCopies = new BufferImageCopy[results.Length];
            for (uint i = 0; i < results.Length; i++)
            {
                uint bufferPosition = i * SingleTextureByteSize;

                imageCopies[i] = new BufferImageCopy(
                    bufferOffset: bufferPosition,
                    bufferRowLength: 0,
                    bufferImageHeight: 0,

                    imageSubresource: new ImageSubresourceLayers(
                        aspectMask: ImageAspectFlags.ColorBit,
                        mipLevel: 0,
                        baseArrayLayer: i,
                        layerCount: 1
                    ),

                    imageOffset: new Offset3D(0, 0, 0),
                    imageExtent: new Extent3D(
                        width: TextureSize,
                        height: TextureSize,
                        depth: 1
                    )
                );
                buffer.Store(bufferPosition, results[i].Data);
            }
            return buffer;
        }

        private unsafe void UploadToImage(VBuffer<byte> stagingBuffer, BufferImageCopy[] imageCopies)
        {
            TransitionImageLayout(ImageLayout.TransferDstOptimal);

            CommandBuffer commandBuffer = _engine.BeginSingleUseCommandBuffer();

            _vk.CmdCopyBufferToImage(commandBuffer, stagingBuffer.Handle, _image, ImageLayout.TransferDstOptimal, (uint)imageCopies.Length, imageCopies);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);
        }

        private unsafe void GenerateMipmaps()
        {
            CommandBuffer commandBuffer = _engine.BeginSingleUseCommandBuffer();

            var barrier = new ImageMemoryBarrier(
                oldLayout: ImageLayout.TransferDstOptimal,
                newLayout: ImageLayout.TransferSrcOptimal,

                srcAccessMask: AccessFlags.TransferWriteBit,
                dstAccessMask: AccessFlags.TransferReadBit,

                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

                image: _image,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: ImageAspectFlags.ColorBit,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: AtlasSize
                )
            );
            _vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, barrier);

            for (int i = 1; i < _mipLevels; i++)
            {
                var imageBlit = new ImageBlit(
                    srcSubresource: new ImageSubresourceLayers(
                        aspectMask: ImageAspectFlags.ColorBit,
                        layerCount: AtlasSize,
                        mipLevel: (uint)(i - 1)
                    ),
                    dstSubresource: new ImageSubresourceLayers(
                        aspectMask: ImageAspectFlags.ColorBit,
                        layerCount: AtlasSize,
                        mipLevel: (uint)i
                    )
                );
                imageBlit.SrcOffsets[1] = new Offset3D(
                    x: (int)(TextureSize >> (i - 1)),
                    y: (int)(TextureSize >> (i - 1)),
                    z: 1
                );
                imageBlit.DstOffsets[1] = new Offset3D(
                    x: (int)(TextureSize >> i),
                    y: (int)(TextureSize >> i),
                    z: 1
                );

                var innerBarrier = new ImageMemoryBarrier(
                    oldLayout: ImageLayout.Undefined,
                    newLayout: ImageLayout.TransferDstOptimal,

                    image: _image,

                    srcAccessMask: 0,
                    dstAccessMask: AccessFlags.TransferWriteBit,

                    srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                    dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

                    subresourceRange: new ImageSubresourceRange(
                        aspectMask: ImageAspectFlags.ColorBit,
                        baseMipLevel: (uint)i,
                        levelCount: 1,
                        baseArrayLayer: 0,
                        layerCount: AtlasSize
                    )
                );
                _vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, innerBarrier);

                _vk.CmdBlitImage(commandBuffer, _image, ImageLayout.TransferSrcOptimal, _image, ImageLayout.TransferDstOptimal, 1, imageBlit, Filter.Linear);

                var undoBarrier = new ImageMemoryBarrier(
                    oldLayout: ImageLayout.TransferDstOptimal,
                    newLayout: ImageLayout.TransferSrcOptimal,

                    image: _image,

                    srcAccessMask: AccessFlags.TransferWriteBit,
                    dstAccessMask: AccessFlags.TransferReadBit,

                    srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                    dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

                    subresourceRange: new ImageSubresourceRange(
                        aspectMask: ImageAspectFlags.ColorBit,
                        baseMipLevel: (uint)i,
                        levelCount: 1,
                        baseArrayLayer: 0,
                        layerCount: AtlasSize
                    )
                );
                _vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, undoBarrier);
            }

            var finalBarrier = new ImageMemoryBarrier(
                oldLayout: ImageLayout.TransferSrcOptimal,
                newLayout: ImageLayout.ShaderReadOnlyOptimal,

                image: _image,

                srcAccessMask: AccessFlags.TransferReadBit,
                dstAccessMask: AccessFlags.ShaderReadBit,

                subresourceRange: new ImageSubresourceRange(
                    aspectMask: ImageAspectFlags.ColorBit,
                    baseMipLevel: 0,
                    levelCount: _mipLevels,
                    baseArrayLayer: 0,
                    layerCount: AtlasSize
                )
            );
            _vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, finalBarrier);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);

            _layout = ImageLayout.ShaderReadOnlyOptimal;
        }

        private unsafe void TransitionImageLayout(ImageLayout newLayout)
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
                    aspectMask: ImageAspectFlags.ColorBit,
                    baseMipLevel: 0,
                    levelCount: _mipLevels,
                    baseArrayLayer: 0,
                    layerCount: AtlasSize
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

            _vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);
            _layout = newLayout;
        }

        private unsafe ImageView CreateImageView()
        {
            var viewInfo = new ImageViewCreateInfo(
                image: _image,
                viewType: ImageViewType.Type2DArray,
                format: Format.R8G8B8A8Srgb,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: ImageAspectFlags.ColorBit,
                    baseMipLevel: 0,
                    levelCount: _mipLevels,
                    baseArrayLayer: 0,
                    layerCount: AtlasSize
                )
            );
            if (_vk.CreateImageView(_engine.Device, viewInfo, null, out ImageView imageView) != Result.Success)
            {
                throw new VulkanException("Failed to create ImageView!");
            }
            return imageView;
        }

        private unsafe Sampler CreateSampler(Filter filter)
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
                compareOp: CompareOp.Never,

                mipmapMode: SamplerMipmapMode.Nearest,
                mipLodBias: 0.0f,
                minLod: 0.0f,
                maxLod: (float)_mipLevels
            );
            if (_vk.CreateSampler(_engine.Device, samplerInfo, null, out Sampler sampler) != Result.Success)
            {
                throw new VulkanException("Failed to create Image-Sampler!");
            }
            return sampler;
        }

        public DescriptorImageInfo ImageInfo()
        {
            return new DescriptorImageInfo(
                imageLayout: _layout,
                imageView: ImageView,
                sampler: Sampler
            );
        }

        public uint TextureIndex(string textureName)
        {
            return _textureArrayIndices[textureName];
        }

        public unsafe void Dispose()
        {
            _vk.DestroySampler(_engine.Device, Sampler, null);
            _vk.DestroyImageView(_engine.Device, ImageView, null);
            _vk.FreeMemory(_engine.Device, _imageMemory, null);
            _vk.DestroyImage(_engine.Device, _image, null);
        }

    }
}
