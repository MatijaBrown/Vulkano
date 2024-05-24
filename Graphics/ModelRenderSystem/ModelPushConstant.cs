using System.Numerics;
using System.Runtime.InteropServices;

namespace Vulkano.Graphics.ModelRenderSystem
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ModelPushConstant
    {

        [FieldOffset(0)]
        public Matrix4x4 Transformation;

        [FieldOffset(64)]
        public Matrix4x4 ViewProjection;

        public ModelPushConstant(Matrix4x4 transformation, Matrix4x4 viewProjection)
        {
            Transformation = transformation;
            ViewProjection = viewProjection;
        }

    }
}
