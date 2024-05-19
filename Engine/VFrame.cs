using Silk.NET.Vulkan;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal class VFrame : IDisposable
    {

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly CommandBuffer _commandBuffer;

        private readonly Silk.NET.Vulkan.Semaphore _imageAvailableSemaphore;
        private readonly Silk.NET.Vulkan.Semaphore _renderFinishedSemaphore;
        private readonly Fence _inFlightFence;

        public VFrame(CommandBuffer commandBuffer, VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;

            _commandBuffer = commandBuffer;
            CreateSyncObjects(out _imageAvailableSemaphore, out _renderFinishedSemaphore, out _inFlightFence);
        }

        private unsafe void CreateSyncObjects(out Silk.NET.Vulkan.Semaphore imageAvailableSemaphore, out Silk.NET.Vulkan.Semaphore renderFinishedSemaphore, out Fence inFlightFence)
        {
            var semaphoreCreateInfo = new SemaphoreCreateInfo(sType: StructureType.SemaphoreCreateInfo);
            var fenceInfo = new FenceCreateInfo(flags: FenceCreateFlags.SignaledBit);

            if ((_vk.CreateSemaphore(_engine.Device, semaphoreCreateInfo, null, out imageAvailableSemaphore) != Result.Success) ||
                (_vk.CreateSemaphore(_engine.Device, semaphoreCreateInfo, null, out renderFinishedSemaphore) != Result.Success) ||
                (_vk.CreateFence(_engine.Device, fenceInfo, null, out inFlightFence) != Result.Success))
            {
                throw new VulkanException("Failed to create synchronisation objects!");
            }
        }

        public unsafe CommandBuffer? Begin()
        {
            VkUtils.AssertVk(_vk.WaitForFences(_engine.Device, 1, _inFlightFence, true, ulong.MaxValue));
            if (!_engine.Swapchain.AcquireNextImage(_imageAvailableSemaphore))
            {
                return null;
            }

            VkUtils.AssertVk(_vk.ResetFences(_engine.Device, 1, _inFlightFence));

            VkUtils.AssertVk(_vk.ResetCommandBuffer(_commandBuffer, 0));
            var beginInfo = new CommandBufferBeginInfo(
                flags: 0,
                pInheritanceInfo: null
            );
            if (_vk.BeginCommandBuffer(_commandBuffer, beginInfo) != Result.Success)
            {
                throw new VulkanException("Failed to begin CommandBuffer!");
            }

            return _commandBuffer;
        }

        public unsafe void End()
        {
            if (_vk.EndCommandBuffer(_commandBuffer) != Result.Success)
            {
                throw new VulkanException("Failed to end recording CommandBuffer!");
            }

            Silk.NET.Vulkan.Semaphore* waitSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[1] { _imageAvailableSemaphore };
            Silk.NET.Vulkan.Semaphore* signalSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[1] { _renderFinishedSemaphore };
            PipelineStageFlags* waitStages = stackalloc PipelineStageFlags[1] { PipelineStageFlags.ColorAttachmentOutputBit };
            var submitInfo = new SubmitInfo(
                waitSemaphoreCount: 1,
                pWaitSemaphores: waitSemaphores,
                pWaitDstStageMask: waitStages,

                commandBufferCount: 1,

                signalSemaphoreCount: 1,
                pSignalSemaphores: signalSemaphores
            );
            fixed (CommandBuffer* ptr = &_commandBuffer)
            {
                submitInfo.PCommandBuffers = ptr;
            }
            Result r;
            if ((r = _vk.QueueSubmit(_engine.GraphicsQueue, 1, submitInfo, _inFlightFence)) != Result.Success)
            {
                throw new VulkanException("Failed to submit draw CommandBuffer! Result: " + r);
            }

            _engine.Swapchain.PresentImage(_renderFinishedSemaphore);
        }

        public unsafe void Dispose()
        {
            _vk.DestroySemaphore(_engine.Device, _imageAvailableSemaphore, null);
            _vk.DestroySemaphore(_engine.Device, _renderFinishedSemaphore, null);
            _vk.DestroyFence(_engine.Device, _inFlightFence, null);
        }

    }
}
