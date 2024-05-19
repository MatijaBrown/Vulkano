using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal unsafe class VBuffer<T> : IDisposable
        where T : unmanaged
    {

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly ulong _instanceSize;
        private readonly ulong _size;

        private readonly DeviceMemory _memory;

        private void* _pData = null;

        public uint Count { get; }

        public Silk.NET.Vulkan.Buffer Handle { get; }

        public unsafe VBuffer(uint count, BufferUsageFlags usage, MemoryPropertyFlags properties, VulkanEngine engine, Vk vk)
        {
            Count = count;
            _engine = engine;
            _vk = vk;

            _instanceSize = (ulong)Marshal.SizeOf<T>();
            _size = Count * _instanceSize;
            Handle = CreateBuffer(usage, properties, out _memory);
        }

        public unsafe VBuffer(ReadOnlySpan<T> data, BufferUsageFlags usage, MemoryPropertyFlags properties, VulkanEngine engine, Vk vk)
            : this((uint)data.Length, usage, properties, engine, vk)
        {
            Map();
            Store(data);
            Unmap();
        }

        private unsafe Silk.NET.Vulkan.Buffer CreateBuffer(BufferUsageFlags usage, MemoryPropertyFlags properties, out DeviceMemory memory)
        {
            var bufferInfo = new BufferCreateInfo(
                size: _size,
                usage: usage,
                sharingMode: SharingMode.Exclusive
            );

            if (_vk.CreateBuffer(_engine.Device, bufferInfo, null, out Silk.NET.Vulkan.Buffer buffer) != Result.Success)
            {
                throw new VulkanException("Failed to create Vertex-Buffer!");
            }

            MemoryRequirements memRequirements = _vk.GetBufferMemoryRequirements(_engine.Device, buffer);
            var allocInfo = new MemoryAllocateInfo(
                allocationSize: memRequirements.Size,
                memoryTypeIndex: VkUtils.FindMemoryType(memRequirements.MemoryTypeBits, properties, _engine.PhysicalDeviceMemoryProperties)
            );
            if (_vk.AllocateMemory(_engine.Device, allocInfo, null, out memory) != Result.Success)
            {
                throw new VulkanException("Failed to allocate Vertex-Buffer-Memory!");
            }

            VkUtils.AssertVk(_vk.BindBufferMemory(_engine.Device, buffer, memory, 0));

            return buffer;
        }

        public unsafe void Map()
        {
            VkUtils.AssertVk(_vk.MapMemory(_engine.Device, _memory, 0, _size, 0, ref _pData));
        }

        public void Unmap()
        {
            _vk.UnmapMemory(_engine.Device, _memory);
            _pData = null;
        }

        public void Store(ReadOnlySpan<T> data, uint offset = 0)
        {
            data.CopyTo(new Span<T>((T*)_pData + offset, (int)Count));
        }

        public void Store(uint offset, params T[] data) => Store((ReadOnlySpan<T>)data, offset);

        public void Store(params T[] data) => Store((ReadOnlySpan<T>)data);

        public T GetAt(uint offset)
        {
            if (_pData == null)
            {
                throw new InvalidOperationException("Tried to get from unmapped Buffer!");
            }
            return *(((T*)_pData) + offset);
        }

        public T* PtrTo(uint offset)
        {
            if (_pData == null)
            {
                throw new InvalidOperationException("Tried to get pointer to unmapped Buffer!");
            }
            return ((T*)_pData) + offset;
        }

        public T this[uint index]
        {
            get => GetAt(index);
            set => Store(index, value);
        }

        public void Flush(uint offset = 0, uint count = 0)
        {
            var flushRange = new MappedMemoryRange(
                memory: _memory,
                offset: offset * _instanceSize,
                size: count == 0 ? Vk.WholeSize : _instanceSize * count
            );
            VkUtils.AssertVk(_vk.FlushMappedMemoryRanges(_engine.Device, 1, flushRange));
        }

        public void CopyTo(VBuffer<T> other, uint srcPosition = 0, uint dstPosition = 0, uint length = 0)
        {
            if (other.Count < Count)
            {
                throw new OverflowException("Buffer Count not sufficient as copy destination!");
            }

            CommandBuffer commandBuffer = _engine.BeginSingleUseCommandBuffer();

            var copyRegion = new BufferCopy(
                srcOffset: srcPosition * _instanceSize,
                dstOffset: dstPosition * _instanceSize,
                size: length == 0 ? _size : length * _instanceSize
            );

            _vk.CmdCopyBuffer(commandBuffer, Handle, other.Handle, 1, copyRegion);

            _engine.EndSingleUseCommandBuffer(commandBuffer, _engine.GraphicsQueue);
        }

        public DescriptorBufferInfo DescriptorInfo(ulong offset = 0, ulong range = 0)
        {
            return new DescriptorBufferInfo(
                buffer: Handle,
                offset: offset * _instanceSize,
                range: range == 0 ? _size : range * _instanceSize
            );
        }

        public void Dispose()
        {
            if (_pData != null)
            {
                Unmap();
            }
            _vk.FreeMemory(_engine.Device, _memory, null);
            _vk.DestroyBuffer(_engine.Device, Handle, null);
        }

        public static VBuffer<T> CreateVertexBuffer(VulkanEngine engine, Vk vk, params T[] data)
        {
            var stagingBuffer = new VBuffer<T>(data, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, engine, vk);
            var vertexBuffer = new VBuffer<T>((uint)data.Length, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, engine, vk);
            stagingBuffer.CopyTo(vertexBuffer);
            stagingBuffer.Dispose();
            return vertexBuffer;
        }

        public static VBuffer<T> CreateIndexBuffer(VulkanEngine engine, Vk vk, params T[] data)
        {
            var stagingBuffer = new VBuffer<T>(data, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, engine, vk);
            var indexBuffer = new VBuffer<T>((uint)data.Length, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, engine, vk);
            stagingBuffer.CopyTo(indexBuffer);
            stagingBuffer.Dispose();
            return indexBuffer;
        }

        public static VBuffer<T> CreateUniformBuffer(VulkanEngine engine, Vk vk)
        {
            var uniformBuffer = new VBuffer<T>(1, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, engine, vk);
            uniformBuffer.Map();
            return uniformBuffer;
        }

    }
}
