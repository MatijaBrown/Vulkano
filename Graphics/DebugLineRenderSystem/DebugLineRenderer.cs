using Silk.NET.Vulkan;
using System.Linq;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    internal class DebugLineRenderer : IRenderer
    {

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly VBuffer<Vector4>[] _linesbuffer;
        private readonly DescriptorSet[] _vDescriptor;

        private readonly uint[] _nVertices;

        public DebugLinePipeline Pipeline { get; }

        public DebugLineRenderer(VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;

            Pipeline = new DebugLinePipeline(_engine, _vk);

            _linesbuffer = new VBuffer<Vector4>[VSwapchain.MAX_FRAMES_IN_FLIGHT];
            _vDescriptor = new DescriptorSet[VSwapchain.MAX_FRAMES_IN_FLIGHT];
            _nVertices = new uint[VSwapchain.MAX_FRAMES_IN_FLIGHT];
            for (uint i = 0; i < _linesbuffer.Length; i++)
            {
                _linesbuffer[i] = new VBuffer<Vector4>(1000, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, _engine, _vk);
                _linesbuffer[i].Map();
                _vDescriptor[i] = _engine.AllocateDescriptor(Pipeline.GetSetLayout(0));
                _engine.WriteDescriptorSet(_vDescriptor[i], 0, _linesbuffer[i].DescriptorInfo(), DescriptorType.UniformBuffer);
                _nVertices[i] = 0;
            }
        }

        public void Render(Camera camera, CommandBuffer cmd)
        {
            uint i = (uint)_engine.Swapchain.CurrentFrameIndex;
            if (_nVertices[i] == 0)
            {
                return;
            }

            Pipeline.Bind(cmd);
            Pipeline.BindDescriptorSet(0, _vDescriptor[i], cmd);
            Pipeline.PushConstants(ShaderStageFlags.VertexBit, camera.ViewProjection, cmd);
            _vk.CmdDraw(cmd, 2, _nVertices[i] / 2, 0, 0);
            _nVertices[i] = 0;
        }

        public void RenderLine((Vector3, Vector3) line)
        {
            for (uint i = 0; i < VSwapchain.MAX_FRAMES_IN_FLIGHT; i++)
            {
                _linesbuffer[i].Store(_nVertices[i]++, new Vector4(line.Item1, 1.0f));
                _linesbuffer[i].Store(_nVertices[i]++, new Vector4(line.Item2, 1.0f));
            }
        }

        public void Dispose()
        {
            for (uint i = 0; i < VSwapchain.MAX_FRAMES_IN_FLIGHT; i++)
            {
                _linesbuffer[i].Dispose();
            }
            Pipeline.Dispose();
        }

    }
}
