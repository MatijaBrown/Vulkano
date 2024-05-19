using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Vulkano.Engine
{
    internal class VUniformValue<T> : IDisposable
        where T : unmanaged
    {

        private readonly VulkanEngine _engine;

        private readonly VBuffer<T> _buffer;
        private readonly uint _memoryIndex;

        private readonly bool _dispose;

        public VUniformValue(VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _memoryIndex = 0;
            _buffer = new VBuffer<T>(VSwapchain.MAX_FRAMES_IN_FLIGHT,
                BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, engine, vk);
            _buffer.Map();
            _dispose = true;
        }

        public VUniformValue(VBuffer<T> buffer, uint memoryIndex, VulkanEngine engine)
        {
            _engine = engine;
            _memoryIndex = memoryIndex;
            _buffer = buffer;
            _dispose = false;
        }

        public void Set(T value)
        {
            _buffer.Store(_memoryIndex * VSwapchain.MAX_FRAMES_IN_FLIGHT + (uint)_engine.Swapchain.CurrentFrameIndex, value);
        }

        public DescriptorBufferInfo BufferInfo()
        {
            return new DescriptorBufferInfo(
                buffer: _buffer.Handle,
                offset: (ulong)(Marshal.SizeOf<T>() * (_memoryIndex * VSwapchain.MAX_FRAMES_IN_FLIGHT + _engine.Swapchain.CurrentFrameIndex)),
                range: (ulong)Marshal.SizeOf<T>()
            );
        }

        public void Dispose()
        {
            if (_dispose)
            {
                _buffer.Dispose();
            }
        }

    }
}
