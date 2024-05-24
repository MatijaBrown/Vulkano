using System.Numerics;

namespace Vulkano.Graphics.ModelRenderSystem
{
    internal struct MeshVertex
    {

        public Vector3 Position;
        public Vector2 TextureCoordinates;

        public MeshVertex(Vector3 position, Vector2 textureCoordinates)
        {
            Position = position;
            TextureCoordinates = textureCoordinates;
        }

        public MeshVertex(float x, float y, float z, float u, float v)
            : this(new Vector3(x, y, z), new Vector2(u, v)) { }

        public MeshVertex(float x, float y, float z, float u, float v, float sx, float sy)
            : this(new Vector3(x, y, z), new Vector2(u / sx, v / sy)) { }

    }
}
