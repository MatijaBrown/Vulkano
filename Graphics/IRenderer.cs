using Silk.NET.Vulkan;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics
{
    internal interface IRenderer : IDisposable
    {

        void Render(Camera camera, CommandBuffer cmd);

    }
}
