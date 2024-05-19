using System.Runtime.InteropServices;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct BlockFace
    {

        [FieldOffset(0)] public uint FaceInfo;

        public BlockFace(uint faceInfo)
        {
            FaceInfo = faceInfo;
        }

    }
}
