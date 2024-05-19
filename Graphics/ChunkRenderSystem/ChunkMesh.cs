using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.World.Blocks;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    internal class ChunkMesh
    {

        public const uint CHUNK_SIZE = 16;
        public const uint CHUNK_AREA = CHUNK_SIZE * CHUNK_SIZE;
        public const uint CHUNK_VOLUME = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
        public const uint MAX_FACES = CHUNK_VOLUME * 6;

        public static uint RebuiltThisFrame { get; set; } = 0;

        public static uint Updates { get; private set; } = 0;

        private readonly World.World _world;
        private readonly uint _chunkWorldX;
        private readonly uint _chunkWorldY;
        private readonly uint _chunkWorldZ;

        private readonly uint _chunkID;

        private readonly ChunkFaceBuilder _builder;

        public bool Dirty { get; private set; } = true;

        public uint FaceCount { get; private set; } = 0;

        public uint FirstFaceOffset => _chunkID * MAX_FACES;

        public Matrix4x4 Translation { get; }

        public ChunkMesh(World.World world, ChunkFaceBuilder builder, uint chunkWorldX, uint chunkWorldY, uint chunkWorldZ, uint chunkID)
        {
            _world = world;
            _builder = builder;
            _chunkWorldX = chunkWorldX;
            _chunkWorldY = chunkWorldY;
            _chunkWorldZ = chunkWorldZ;
            _chunkID = chunkID;

            Translation = Matrix4x4.CreateTranslation(_chunkWorldX, _chunkWorldY, _chunkWorldZ);
        }

        public void Rebuild(uint layer, TextureAtlas atlas)
        {
            if (!Dirty)
            {
                return;
            }

            Dirty = false;
            Updates++;
            RebuiltThisFrame++;
            _builder.BeginChunk(_chunkID);
            for (uint y = 0; y < CHUNK_SIZE; y++)
            {
                for (uint x = 0; x < CHUNK_SIZE; x++)
                {
                    for (uint z = 0; z < CHUNK_SIZE; z++)
                    {
                        uint worldX = _chunkWorldX + x;
                        uint worldY = _chunkWorldY + y;
                        uint worldZ = _chunkWorldZ + z;
                        if (!_world.IsBlock(worldX, worldY, worldZ))
                        {
                            continue;
                        }
                        bool tex = worldY == _world.Height * 2 / 3;
                        if (!tex)
                        {
                            Block.Stone.BuildFaces(_builder, atlas, _world, layer, x, y, z, worldX, worldY, worldZ);
                        }
                        else
                        {
                            Block.Grass.BuildFaces(_builder, atlas, _world, layer, x, y, z, worldX, worldY, worldZ);
                        }
                    }
                }
            }
            FaceCount = _builder.Flush();
        }

        public void MarkDirty()
        {
            Dirty = true;
        }

    }
}
