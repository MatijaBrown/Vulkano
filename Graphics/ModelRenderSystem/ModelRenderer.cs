using Silk.NET.Vulkan;
using Vulkano.Engine;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics.ModelRenderSystem
{
    internal class ModelRenderer : IRenderer
    {

        private readonly IDictionary<Model, IList<Transform>> _instances = new Dictionary<Model, IList<Transform>>();

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        public ModelPipeline Pipeline { get; }

        public ModelRenderer(VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;

            Pipeline = new ModelPipeline(_engine, _vk);
        }

        public void RenderModel(Model model, Transform transform)
        {
            if (!_instances.TryGetValue(model, out IList<Transform>? transforms))
            {
                transforms = new List<Transform>();
                _instances.Add(model, transforms);
            }
            transforms.Add(transform);
        }

        public void Render(Camera camera, CommandBuffer cmd)
        {
            Pipeline.Bind(cmd);

            foreach (Model model in _instances.Keys)
            {
                _vk.CmdBindVertexBuffers(cmd, 0, 1, model.Mesh.VertexBuffer.Handle, 0);
                _vk.CmdBindIndexBuffer(cmd, model.Mesh.IndexBuffer.Handle, 0, IndexType.Uint16);
                Pipeline.BindDescriptorSet(0, model.Texture, cmd);
                foreach (Transform transform in _instances[model])
                {
                    var pushConstants = new ModelPushConstant(transform.TransformationMatrix, camera.ViewProjection);
                    Pipeline.PushConstants(ShaderStageFlags.VertexBit, pushConstants, cmd);
                    _vk.CmdDrawIndexed(cmd, model.Mesh.IndexBuffer.Count, 1, 0, 0, 0);
                }
            }

            _instances.Clear();
        }
        
        public void Dispose()
        {
            Pipeline.Dispose();
        }

    }
}
