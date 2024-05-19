using System.Numerics;
using System.Runtime.InteropServices;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct DebugLinePushConstant
    {

        [FieldOffset(0)]
        public Matrix4x4 ModelViewProjection;

        public DebugLinePushConstant(Matrix4x4 modelViewProjection)
        {
            ModelViewProjection = modelViewProjection;
        }

    }
}
