using Silk.NET.Vulkan;

namespace Vulkano.Graphics.ModelRenderSystem
{
    internal class Model : IDisposable
    {

        public Mesh Mesh { get; }

        public DescriptorSet Texture { get; }

        public Model(Mesh mesh, DescriptorSet texture)
        {
            Mesh = mesh;
            Texture = texture;
        }

        public void Dispose()
        {
            Mesh.Dispose();
        }

    }
}
