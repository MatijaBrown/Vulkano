using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Utils;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    internal class SelectionRenderer : IRenderer
    {

        private readonly IList<HitResult> _hits = new List<HitResult>();

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly VBuffer<ushort> _indexBuffer;

        public SelectionPipeline Pipeline { get; }

        public SelectionRenderer(VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;

            Pipeline = new SelectionPipeline(_engine, _vk);

            _indexBuffer = VBuffer<ushort>.CreateIndexBuffer(_engine, _vk, 0, 1, 2, 0, 2, 3);
        }

        public void Render(Camera camera, CommandBuffer cmd)
        {
            if (_hits.Count == 0)
            {
                return;
            }

            Pipeline.Bind(cmd);
            _vk.CmdBindIndexBuffer(cmd, _indexBuffer.Handle, 0, IndexType.Uint16);
            foreach (HitResult hit in _hits)
            {
                Face face = hit.GetFace();
                var pushConstant = new SelectionPushConstant(
                    modelViewProjection: Matrix4x4.CreateTranslation(new Vector3(hit.X, hit.Y, hit.Z) + face.Normal * 0.005f) * camera.ViewProjection,
                    face: face.Index,
                    alpha: (float)Math.Sin(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 100.0) * 0.2f + 0.4f
                );

                Pipeline.PushConstants(ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, pushConstant, cmd);
                _vk.CmdDrawIndexed(cmd, 6, 1, 0, 0, 1);
            }
            _hits.Clear();
        }

        public void RenderHit(HitResult hit)
        {
            _hits.Add(hit);
        }

        public void Dispose()
        {
            _indexBuffer.Dispose();
            Pipeline.Dispose();
        }

    }
}
