using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{

    internal class VSwapchain : IDisposable
    {

        public const int MAX_FRAMES_IN_FLIGHT = 2;

        private readonly Display _display;
        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly KhrSwapchain _extSwapchain;

        private SwapchainSupportDetails _swapchainDetails;

        private Image[] _swapchainImages;
        private ImageView[] _imageViews;
        private VImage[] _depthImages;
        private Framebuffer[] _framebuffers;

        private uint _swapchainImageIndex;

        private bool _resized = false;

        public SurfaceFormatKHR SurfaceFormat { get; private set; }

        public Format ImageFormat => SurfaceFormat.Format;

        public PresentModeKHR PresentMode { get; private set; }

        public Extent2D Extent { get; private set; }

        public SwapchainKHR Handle { get; private set; }

        public RenderPass RenderPass { get; }

        public VFrame[] Frames { get; }

        public int CurrentFrameIndex { get; private set; }

        public VSwapchain(Display display, VulkanEngine engine, Vk vk)
        {
            _display = display;
            _engine = engine;
            _vk = vk;

            FindSwapchainDetails();
            _extSwapchain = GetSwapchainExtension();
            Handle = CreateSwapchain(out _swapchainImages);
            _imageViews = CreateSwapchainImageViews();
            _depthImages = CreateDepthImages();

            RenderPass = CreateRenderPass();

            _framebuffers = CreateFramebuffers();

            _display.OnResize += FlagResized;

            var commandBuffers = _engine.AllocateCommandBuffers(MAX_FRAMES_IN_FLIGHT);
            Frames = new VFrame[MAX_FRAMES_IN_FLIGHT];
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                Frames[i] = new VFrame(commandBuffers[i], _engine, _vk);
            }
            CurrentFrameIndex = -1;
        }

        private void FindSwapchainDetails()
        {
            _swapchainDetails = _engine.QuerySwapchainSupport();
            SurfaceFormat = ChooseSwapSurfaceFormat();
            PresentMode = ChooseSwapPresentMode();
            Extent = ChooseSwapExtent();
        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat()
        {
            foreach (SurfaceFormatKHR availableFormat in _swapchainDetails.Formats)
            {
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.PaceSrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }

            return _swapchainDetails.Formats[0];
        }

        private PresentModeKHR ChooseSwapPresentMode()
        {
            foreach (PresentModeKHR availablePresentMode in _swapchainDetails.PresentModes)
            {
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.FifoKhr;
        }

        private Extent2D ChooseSwapExtent()
        {
            if (_swapchainDetails.Capabilities.MaxImageExtent.Width != uint.MaxValue)
            {
                return _swapchainDetails.Capabilities.MaxImageExtent;
            }
            else
            {
                Extent2D actualExtent = _display.FramebufferExtent;

                actualExtent.Width = Math.Max(_swapchainDetails.Capabilities.MinImageExtent.Width, Math.Min(actualExtent.Width, _swapchainDetails.Capabilities.MaxImageExtent.Width));
                actualExtent.Height = Math.Max(_swapchainDetails.Capabilities.MinImageExtent.Height, Math.Min(actualExtent.Height, _swapchainDetails.Capabilities.MaxImageExtent.Height));

                return actualExtent;
            }
        }

        private unsafe KhrSwapchain GetSwapchainExtension()
        {
            if (!_vk.TryGetDeviceExtension(_engine.Instance, _engine.Device, out KhrSwapchain extSwapchain))
            {
                throw new VulkanException("Failed to get KhrSwapchain-Extension!");
            }
            return extSwapchain;
        }

        private unsafe SwapchainKHR CreateSwapchain(out Image[] swapchainImages)
        {
            uint imageCount = _swapchainDetails.Capabilities.MinImageCount + 1;
            if (_swapchainDetails.Capabilities.MaxImageCount > 0 && imageCount > _swapchainDetails.Capabilities.MaxImageCount)
            {
                imageCount = _swapchainDetails.Capabilities.MaxImageCount;
            }

            var createInfo = new SwapchainCreateInfoKHR(
                surface: _engine.Surface,

                minImageCount: imageCount,
                imageFormat: SurfaceFormat.Format,
                imageColorSpace: SurfaceFormat.ColorSpace,
                imageExtent: Extent,
                imageArrayLayers: 1,
                imageUsage: ImageUsageFlags.ColorAttachmentBit,

                preTransform: _swapchainDetails.Capabilities.CurrentTransform,

                compositeAlpha: CompositeAlphaFlagsKHR.OpaqueBitKhr,

                presentMode: PresentMode,
                clipped: true,

                oldSwapchain: null
            );

            QueueFamilyIndicies indices = _engine.QueueFamilyIndicies;
            uint* relevantIndices = stackalloc uint[2] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = relevantIndices;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
                createInfo.QueueFamilyIndexCount = 0;
                createInfo.PQueueFamilyIndices = null;
            }

            if (_extSwapchain.CreateSwapchain(_engine.Device, createInfo, null, out SwapchainKHR swapchain) != Result.Success)
            {
                throw new VulkanException("Failed to create Swapchain!");
            }

            VkUtils.AssertVk(_extSwapchain.GetSwapchainImages(_engine.Device, swapchain, ref imageCount, null));
            swapchainImages = new Image[imageCount];
            VkUtils.AssertVk(_extSwapchain.GetSwapchainImages(_engine.Device, swapchain, ref imageCount, out swapchainImages[0]));

            return swapchain;
        }

        private unsafe ImageView[] CreateSwapchainImageViews()
        {
            ImageView[] swapchainImageViews = new ImageView[_swapchainImages.Length];
            for (int i = 0; i < _swapchainImages.Length; i++)
            {
                var createInfo = new ImageViewCreateInfo(
                    image: _swapchainImages[i],

                    viewType: ImageViewType.Type2D,
                    format: ImageFormat,

                    components: new ComponentMapping(
                        r: ComponentSwizzle.Identity,
                        g: ComponentSwizzle.Identity,
                        b: ComponentSwizzle.Identity,
                        a: ComponentSwizzle.Identity
                    ),

                    subresourceRange: new ImageSubresourceRange(
                        aspectMask: ImageAspectFlags.ColorBit,
                        baseMipLevel: 0,
                        levelCount: 1,
                        baseArrayLayer: 0,
                        layerCount: 1
                    )
                );

                if (_vk.CreateImageView(_engine.Device, createInfo, null, out swapchainImageViews[i]) != Result.Success)
                {
                    throw new VulkanException("Failed to create Swapchain-ImageView!");
                }
            }
            return swapchainImageViews;
        }

        private Format FindDepthFormat()
        {
            return VkUtils.FindSupportedFormat(ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit,
                _engine.PhysicalDevice, _vk, Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint);
        }

        private unsafe VImage[] CreateDepthImages()
        {
            Format depthFormat = FindDepthFormat();
            var depthImages = new VImage[_swapchainImages.Length];
            for (int i = 0; i < depthImages.Length; i++)
            {
                depthImages[i] = new VImage(Extent.Width, Extent.Height, depthFormat, ImageUsageFlags.DepthStencilAttachmentBit, _engine, _vk);
                depthImages[i].TransitionImageLayout(ImageLayout.DepthStencilAttachmentOptimal);
            }
            return depthImages;
        }

        private unsafe RenderPass CreateRenderPass()
        {
            AttachmentDescription* attachments = stackalloc AttachmentDescription[2]
            {
                new AttachmentDescription(
                    format: ImageFormat,
                    samples: SampleCountFlags.Count1Bit,

                    loadOp: AttachmentLoadOp.Clear,
                    storeOp: AttachmentStoreOp.Store,

                    stencilLoadOp: AttachmentLoadOp.DontCare,
                    stencilStoreOp: AttachmentStoreOp.DontCare,

                    initialLayout: ImageLayout.Undefined,
                    finalLayout: ImageLayout.PresentSrcKhr
                ),
                new AttachmentDescription(
                    format: FindDepthFormat(),
                    samples: SampleCountFlags.Count1Bit,

                    loadOp: AttachmentLoadOp.Clear,
                    storeOp: AttachmentStoreOp.DontCare,

                    stencilLoadOp: AttachmentLoadOp.DontCare,
                    stencilStoreOp: AttachmentStoreOp.DontCare,

                    initialLayout: ImageLayout.Undefined,
                    finalLayout: ImageLayout.DepthStencilAttachmentOptimal
                )
            };

            var colourAttachmentRef = new AttachmentReference(
                attachment: 0,
                layout: ImageLayout.ColorAttachmentOptimal
            );

            var depthAttachmentRef = new AttachmentReference(
                attachment: 1,
                layout: ImageLayout.DepthStencilAttachmentOptimal
            );

            var subpass = new SubpassDescription(
                pipelineBindPoint: PipelineBindPoint.Graphics,
                colorAttachmentCount: 1,
                pColorAttachments: &colourAttachmentRef,
                pDepthStencilAttachment: &depthAttachmentRef
            );

            var dependency = new SubpassDependency(
                srcSubpass: Vk.SubpassExternal,
                dstSubpass: 0,

                srcStageMask: PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                srcAccessMask: 0,

                dstStageMask: PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                dstAccessMask: AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            );

            var renderPassCreateInfo = new RenderPassCreateInfo(
                attachmentCount: 2,
                pAttachments: attachments,
                subpassCount: 1,
                pSubpasses: &subpass,
                dependencyCount: 1,
                pDependencies: &dependency
            );

            if (_vk.CreateRenderPass(_engine.Device, renderPassCreateInfo, null, out RenderPass renderPass) != Result.Success)
            {
                throw new VulkanException("Failed to create RenderPass!");
            }

            return renderPass;
        }

        private unsafe Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[_swapchainImages.Length];
            ImageView* attachments = stackalloc ImageView[2];
            for (int i = 0; i < _swapchainImages.Length; i++)
            {
                attachments[0] = _imageViews[i];
                attachments[1] = _depthImages[i].ImageView;

                var framebufferCreateInfo = new FramebufferCreateInfo(
                    renderPass: RenderPass,
                    attachmentCount: 2,
                    pAttachments: attachments,
                    width: Extent.Width,
                    height: Extent.Height,
                    layers: 1
                );

                if (_vk.CreateFramebuffer(_engine.Device, framebufferCreateInfo, null, out framebuffers[i]) != Result.Success)
                {
                    throw new VulkanException("Failed to create Framebuffer!");
                }
            }
            return framebuffers;
        }

        private unsafe void CleanUpSwapchain()
        {
            foreach (Framebuffer framebuffer in _framebuffers)
            {
                _vk.DestroyFramebuffer(_engine.Device, framebuffer, null);
            }
            foreach (VImage depthImage in _depthImages)
            {
                depthImage.Dispose();
            }
            foreach (ImageView imageView in _imageViews)
            {
                _vk.DestroyImageView(_engine.Device, imageView, null);
            }
            _extSwapchain.DestroySwapchain(_engine.Device, Handle, null);
        }

        private unsafe void RecreateSwapchain()
        {
            VkUtils.AssertVk(_vk.DeviceWaitIdle(_engine.Device));

            FindSwapchainDetails();

            if (Extent.Width == 0 || Extent.Height == 0)
            {
                return;
            }

            CleanUpSwapchain();

            Handle = CreateSwapchain(out _swapchainImages);
            _imageViews = CreateSwapchainImageViews();
            _depthImages = CreateDepthImages();
            _framebuffers = CreateFramebuffers();
        }

        private void FlagResized(uint width, uint height)
        {
            _resized = true;
        }

        public VFrame GetNextFrame()
        {
            CurrentFrameIndex = (CurrentFrameIndex + 1) % MAX_FRAMES_IN_FLIGHT;
            return Frames[CurrentFrameIndex];
        }

        public unsafe void BeginRenderPass(CommandBuffer cmd)
        {
            ClearValue* clearValues = stackalloc ClearValue[2]
            {
                new ClearValue(color: new ClearColorValue(0.1f, 0.5f, 1.0f, 1.0f)),
                new ClearValue(depthStencil: new ClearDepthStencilValue(1.0f, 0))
            };

            var renderPassBeginInfo = new RenderPassBeginInfo(
                renderPass: RenderPass,
                framebuffer: _framebuffers[_swapchainImageIndex],

                renderArea: new Rect2D(
                    offset: new Offset2D(0, 0),
                    extent: Extent
                ),

                clearValueCount: 2,
                pClearValues: clearValues
            );
            _vk.CmdBeginRenderPass(cmd, renderPassBeginInfo, SubpassContents.Inline);

            var viewport = new Viewport(
                x: 0.0f,
                y: 0.0f,
                width: Extent.Width,
                height: Extent.Height,
                minDepth: 0.0f,
                maxDepth: 1.0f
            );
            _vk.CmdSetViewport(cmd, 0, 1, viewport);

            var scissor = new Rect2D(
                offset: new Offset2D(0, 0),
                extent: Extent
            );
            _vk.CmdSetScissor(cmd, 0, 1, scissor);
        }

        public void EndRenderPass(CommandBuffer cmd)
        {
            _vk.CmdEndRenderPass(cmd);
        }

        public unsafe bool AcquireNextImage(Silk.NET.Vulkan.Semaphore imageAvailableSemaphore)
        {
            Result result = _extSwapchain.AcquireNextImage(_engine.Device, Handle, ulong.MaxValue, imageAvailableSemaphore, new Fence(handle: null), ref _swapchainImageIndex);
            if (result == Result.ErrorOutOfDateKhr)
            {
                RecreateSwapchain();
                return false;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
            {
                throw new VulkanException("Failed to acquire next Swapchain-Image!");
            }
            return true;
        }

        public unsafe void PresentImage(Silk.NET.Vulkan.Semaphore waitSemaphore)
        {
            SwapchainKHR* swapchains = stackalloc SwapchainKHR[1] { Handle };
            uint* imageIndices = stackalloc uint[] { _swapchainImageIndex };

            var presentInfo = new PresentInfoKHR(
                waitSemaphoreCount: 1,
                pWaitSemaphores: &waitSemaphore,

                swapchainCount: 1,
                pSwapchains: swapchains,
                pImageIndices: imageIndices,

                pResults: null
            );

            Result result = _extSwapchain.QueuePresent(_engine.PresentQueue, presentInfo);
            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _resized)
            {
                _resized = false;
                RecreateSwapchain();
            }
            else if (result != Result.Success)
            {
                throw new VulkanException("Failed to present Swapchain-Image!");
            }
        }

        public unsafe void Dispose()
        {
            foreach (VFrame frame in Frames)
            {
                frame.Dispose();
            }
            CleanUpSwapchain();
            _vk.DestroyRenderPass(_engine.Device, RenderPass, null);
            _extSwapchain.Dispose();
        }

        internal readonly struct SwapchainSupportDetails
        {
            public readonly SurfaceCapabilitiesKHR Capabilities;
            public readonly SurfaceFormatKHR[] Formats;
            public readonly PresentModeKHR[] PresentModes;

            public bool IsAdequate => Formats.Length > 0 && PresentModes.Length > 0;

            public SwapchainSupportDetails(SurfaceCapabilitiesKHR capabilities, SurfaceFormatKHR[] formats, PresentModeKHR[] presentModes)
            {
                Capabilities = capabilities;
                Formats = formats;
                PresentModes = presentModes;
            }
        }

    }
}
