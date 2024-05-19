using System.Numerics;
using System.Runtime.InteropServices;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ChunkPushConstant
    {

        [FieldOffset(0)]
        public Matrix4x4 Transformation;
        [FieldOffset(64)]
        public uint FirstFaceIndex;

        public ChunkPushConstant(Matrix4x4 transformation, uint firstFaceIndex)
        {
            Transformation = transformation;
            FirstFaceIndex = firstFaceIndex;
        }

    }
}
