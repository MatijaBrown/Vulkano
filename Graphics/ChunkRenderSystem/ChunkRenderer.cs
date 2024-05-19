using Silk.NET.Vulkan;
using Vulkano.Engine;
using Vulkano.Utils.Maths;
using Vulkano.World;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    internal class ChunkRenderer : IRenderer, IWorldChangeListener
    {

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly World.World _world;

        private readonly ChunkMesh[] _chunkMeshes;

        private readonly uint _chunkWidth;
        private readonly uint _chunkDepth;
        private readonly uint _chunkHeight;

        private readonly VBuffer<BlockFace> _faces;
        private readonly DescriptorSet _facesDescriptor;
        private readonly ChunkFaceBuilder _faceBuilder;

        private readonly VBuffer<ushort> _indexBuffer;

        private readonly TextureAtlas _textures;
        private readonly DescriptorSet _textureDescriptor;

        public ChunkPipeline Pipeline { get; }

        public ChunkRenderer(World.World world, VulkanEngine engine, Vk vk)
        {
            _engine = engine;
            _vk = vk;
            _world = world;

            _world.AddChangeListener(this);

            Pipeline = new ChunkPipeline(_engine, _vk);

            _chunkWidth = _world.Width / ChunkMesh.CHUNK_SIZE;
            _chunkDepth = _world.Depth / ChunkMesh.CHUNK_SIZE;
            _chunkHeight = _world.Height / ChunkMesh.CHUNK_SIZE;

            _textures = new TextureAtlas(typeof(Resources.Terrain), 5, Filter.Nearest, _engine, _vk);
            _textureDescriptor = _engine.AllocateDescriptor(Pipeline.GetSetLayout(1));
            _engine.WriteDescriptorSet(_textureDescriptor, 0, _textures.ImageInfo());

            _indexBuffer = VBuffer<ushort>.CreateIndexBuffer(_engine, _vk, 0, 1, 2, 0, 2, 3);

            _chunkMeshes = new ChunkMesh[_chunkWidth * _chunkDepth * _chunkHeight];
            _faces = new VBuffer<BlockFace>(_chunkWidth * _chunkDepth * _chunkHeight * ChunkMesh.MAX_FACES,
                BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, _engine, _vk); ;
            _facesDescriptor = _engine.AllocateDescriptor(Pipeline.GetSetLayout(0));
            _engine.WriteDescriptorSet(_facesDescriptor, 0, _faces.DescriptorInfo(), DescriptorType.StorageBuffer);

            _faceBuilder = new ChunkFaceBuilder(_faces, _engine, _vk);
            for (uint y = 0; y < _chunkHeight; y++)
            {
                for (uint x = 0; x < _chunkWidth; x++)
                {
                    for (uint z = 0; z < _chunkDepth; z++)
                    {
                        uint chunkWorldX = x * ChunkMesh.CHUNK_SIZE;
                        uint chunkWorldY = y * ChunkMesh.CHUNK_SIZE;
                        uint chunkWorldZ = z * ChunkMesh.CHUNK_SIZE;

                        uint chunkID = y * _chunkWidth * _chunkDepth + x * _chunkDepth + z;

                        _chunkMeshes[chunkID] = new ChunkMesh(_world, _faceBuilder, chunkWorldX, chunkWorldY, chunkWorldZ, chunkID);
                    }
                }
            }
        }

        private void MarkDirty(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            int chunkX0 = Math.Max(x0 / (int)ChunkMesh.CHUNK_SIZE, 0);
            int chunkY0 = Math.Max(y0 / (int)ChunkMesh.CHUNK_SIZE, 0);
            int chunkZ0 = Math.Max(z0 / (int)ChunkMesh.CHUNK_SIZE, 0);

            int chunkX1 = Math.Min(x1 / (int)ChunkMesh.CHUNK_SIZE, (int)_chunkWidth - 1);
            int chunkY1 = Math.Min(y1 / (int)ChunkMesh.CHUNK_SIZE, (int)_chunkHeight - 1);
            int chunkZ1 = Math.Min(z1 / (int)ChunkMesh.CHUNK_SIZE, (int)_chunkDepth - 1);

            for (uint cy = (uint)chunkY0; cy <= chunkY1; cy++)
            {
                for (uint cx = (uint)chunkX0; cx <= chunkX1; cx++)
                {
                    for (uint cz = (uint)chunkZ0; cz <= chunkZ1; cz++)
                    {
                        _chunkMeshes[cy * _chunkWidth * _chunkDepth + cx * _chunkDepth + cz].MarkDirty();
                    }
                }
            }
        }

        public void BlockChanged(uint x, uint y, uint z)
        {
            MarkDirty((int)x - 1, (int)y - 1, (int)z - 1, (int)x + 1, (int)y + 1, (int)z + 1);
        }

        public void LightColumnChanged(uint x, uint z, uint yFrom, uint yTo)
        {
            MarkDirty((int)x - 1, (int)yFrom - 1, (int)z - 1, (int)x + 1, (int)yTo + 1, (int)z + 1);
        }

        public void AllChanged()
        {
            MarkDirty(0, 0, 0, (int)_world.Width, (int)_world.Height, (int)_world.Depth);
        }

        public void Render(Camera camera, CommandBuffer cmd)
        {
            ChunkMesh.RebuiltThisFrame = 0;
            Pipeline.Bind(cmd);
            Pipeline.BindDescriptorSet(0, _facesDescriptor, cmd);
            Pipeline.BindDescriptorSet(1, _textureDescriptor, cmd);
            _vk.CmdBindIndexBuffer(cmd, _indexBuffer.Handle, 0, IndexType.Uint16);
            foreach (ChunkMesh chunk in _chunkMeshes)
            {
                if (chunk.Dirty)
                {
                    chunk.Rebuild(0, _textures);
                }

                var pushConstant = new ChunkPushConstant(chunk.Translation * camera.ViewMatrix * camera.ProjectionMatrix, chunk.FirstFaceOffset);
                Pipeline.PushConstants(ShaderStageFlags.VertexBit, pushConstant, cmd);

                _vk.CmdDrawIndexed(cmd, 6, chunk.FaceCount, 0, 0, 0);
            }
        }

        public void Dispose()
        {
            _textures.Dispose();
            _indexBuffer.Dispose();
            _faceBuilder.Dispose();
            _faces.Dispose();
            Pipeline.Dispose();
        }

    }
}
