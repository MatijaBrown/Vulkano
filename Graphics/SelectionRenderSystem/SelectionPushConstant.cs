using System.Numerics;
using System.Runtime.InteropServices;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct SelectionPushConstant
    {

        [FieldOffset(0)]
        public Matrix4x4 ModelViewProjection;

        [FieldOffset(64)]
        public uint Face;

        [FieldOffset(68)]
        public float Alpha;

        public SelectionPushConstant(Matrix4x4 modelViewProjection, uint face, float alpha)
        {
            ModelViewProjection = modelViewProjection;
            Face = face;
            Alpha = alpha;
        }

    }
}
