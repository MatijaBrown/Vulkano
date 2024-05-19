using Vulkano.Graphics;
using Vulkano.Graphics.ChunkRenderSystem;
using Vulkano.Utils.Maths;

namespace Vulkano.World.Blocks
{
    internal class Block
    {

        public static Block Grass { get; } = new("Grass");

        public static Block Stone { get; } = new("Stone");

        private readonly string _textureName;

        private Block(string textureName)
        {
            _textureName = textureName;
        }

        public void BuildFaces(ChunkFaceBuilder b, TextureAtlas atlas, World world, uint layer, uint x, uint y, uint z, uint worldX, uint worldY, uint worldZ)
        {
            uint texture = atlas.TextureIndex(_textureName);
            if (world.IsTransparent(worldX, worldY - 1, worldZ))
            {
                b.LightLevel(world.GetLightLevel(worldX, worldY - 1, worldZ));
                b.Face(Face.Bottom);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
            if (world.IsTransparent(worldX, worldY + 1, worldZ))
            {
                b.LightLevel(world.GetLightLevel(worldX, worldY + 1, worldZ));
                b.Face(Face.Top);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
            if (world.IsTransparent(worldX - 1, worldY, worldZ))
            {
                b.LightLevel(world.GetLightLevel(worldX - 1, worldY, worldZ));
                b.Face(Face.West);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
            if (world.IsTransparent(worldX + 1, worldY, worldZ))
            {
                b.LightLevel(world.GetLightLevel(worldX + 1, worldY, worldZ));
                b.Face(Face.East);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
            if (world.IsTransparent(worldX, worldY, worldZ - 1))
            {
                b.LightLevel(world.GetLightLevel(worldX, worldY, worldZ - 1));
                b.Face(Face.South);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
            if (world.IsTransparent(worldX, worldY, worldZ + 1))
            {
                b.LightLevel(world.GetLightLevel(worldX, worldY, worldZ + 1));
                b.Face(Face.North);
                b.Tex(texture);
                b.Position(x, y, z);
                b.StoreFace();
            }
        }

    }
}
