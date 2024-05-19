using Silk.NET.Vulkan;
using Vulkano.Engine;
using Vulkano.Graphics.ChunkRenderSystem;
using Vulkano.Graphics.SelectionRenderSystem;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics
{
    internal class MasterRenderer : IDisposable
    {

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        public  ChunkRenderer? ChunkRenderer { get; set; }

        public SelectionRenderer? SelectionRenderer { get; set; }

        public DebugLineRenderer? DebugLineRenderer { get; set; }

        public MasterRenderer(VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;
        }

        private void ClearLists()
        {

        }

        public unsafe void Render(Camera camera)
        {
            VFrame frame = _engine.Swapchain.GetNextFrame();
            CommandBuffer? cmd = frame.Begin();

            if (!cmd.HasValue)
            {
                return;
            }

            _engine.Swapchain.BeginRenderPass(cmd.Value);

            ChunkRenderer?.Render(camera, cmd.Value);
            SelectionRenderer?.Render(camera, cmd.Value);
            DebugLineRenderer?.Render(camera, cmd.Value);

            _engine.Swapchain.EndRenderPass(cmd.Value);

            frame.End();

            ClearLists();
        }

        public unsafe void Dispose()
        {
            DebugLineRenderer?.Dispose();
            SelectionRenderer?.Dispose();
            ChunkRenderer?.Dispose();
        }

    }
}
