using Silk.NET.Vulkan;
using Vulkano.Engine;

namespace Vulkano.Graphics.ModelRenderSystem
{
    internal class Mesh : IDisposable
    {

        private readonly VulkanEngine _engine;

        public VBuffer<MeshVertex> VertexBuffer { get; }

        public VBuffer<ushort> IndexBuffer { get; }

        public Mesh(uint vertexCount, uint indexCount, VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            VertexBuffer = new VBuffer<MeshVertex>(vertexCount, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, _engine, vk);
            IndexBuffer = new VBuffer<ushort>(indexCount, BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, _engine, vk);
        }

        public Mesh(VulkanEngine engine, Vk vk, MeshVertex[] vertices, ushort[] indices)
            : this((uint)vertices.Length, (uint)indices.Length, engine, vk)
        {
            LoadVertices(vertices);
            LoadIndices(indices);
        }

        public void LoadVertices(params MeshVertex[] vertices)
        {
            if (vertices.Length > VertexBuffer.Count)
            {
                throw new ArgumentException("Too many vertices specified!");
            }
            var stagingBuffer = _engine.StagingBuffer<MeshVertex>((uint)vertices.Length);
            stagingBuffer.Map();
            stagingBuffer.Store(vertices);
            stagingBuffer.CopyTo(VertexBuffer);
            stagingBuffer.Dispose();
        }

        public void LoadIndices(params ushort[] indices)
        {
            if (indices.Length > IndexBuffer.Count)
            {
                throw new ArgumentException("Too many indices specified!");
            }
            var stagingBuffer = _engine.StagingBuffer<ushort>((uint)indices.Length);
            stagingBuffer.Map();
            stagingBuffer.Store(indices);
            stagingBuffer.CopyTo(IndexBuffer);
            stagingBuffer.Dispose();
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }

    }
}
