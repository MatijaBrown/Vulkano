using Silk.NET.Vulkan;
using Vulkano.Engine;
using Vulkano.Utils.Maths;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    internal class ChunkFaceBuilder : IDisposable
    {
        private const uint POSITION_MASK = ChunkMesh.CHUNK_VOLUME - 1;
        private const uint FACE_INDEX_MASK = (0b111) << 12;
        private const uint LIGHT_LEVEL_MASK = (0b1111) << 15;
        private const uint TEXTURE_MASK = ~(0 | LIGHT_LEVEL_MASK | POSITION_MASK | FACE_INDEX_MASK);

        private readonly VBuffer<BlockFace> _worldBuffer;
        private readonly VBuffer<BlockFace> _stagingBuffer;

        private uint _faceCount;
        private uint _chunkOffset;

        private uint _position;
        private uint _faceIndex;
        private uint _lightLevel;
        private uint _textureIndex;

        public ChunkFaceBuilder(VBuffer<BlockFace> worldBuffer, VulkanEngine engine, Vk vk)
        {
            _worldBuffer = worldBuffer;
            _stagingBuffer = new VBuffer<BlockFace>(ChunkMesh.MAX_FACES, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit, engine, vk);
            _stagingBuffer.Map();
        }

        public void Clear()
        {
            _position = 0;
            _faceIndex = 0;
            _lightLevel = 0;
            _textureIndex = 0;
            _faceCount = 0;
        }

        public void BeginChunk(uint chunkID)
        {
            Clear();
            _chunkOffset = chunkID * ChunkMesh.MAX_FACES;
        }

        public void StoreFace()
        {
            uint pos = (_position & POSITION_MASK) << 0;
            uint faceIndex = (_faceIndex & (FACE_INDEX_MASK >> 12)) << 12;
            uint lightLevel = (_lightLevel & (LIGHT_LEVEL_MASK >> 15)) << 15;
            uint texMask = (_textureIndex & (TEXTURE_MASK >> 19)) << 19;

            _stagingBuffer[_faceCount++] = new BlockFace(pos | faceIndex | lightLevel | texMask);
        }

        public uint Flush()
        {
            _stagingBuffer.Flush(0, 0);
            _stagingBuffer.CopyTo(_worldBuffer, 0, _chunkOffset, ChunkMesh.MAX_FACES);
            return _faceCount;
        }

        public void Tex(uint textureIndex)
        {
            _textureIndex = textureIndex;
        }

        public void LightLevel(uint lightLevel)
        {
            _lightLevel = lightLevel;
        }

        public void Face(uint faceIndex)
        {
            _faceIndex = faceIndex;
        }

        public void Face(Face face)
        {
            Face(face.Index);
        }

        public void Position(uint position)
        {
            _position = position;
        }

        public void Position(uint chunkX, uint chunkY, uint chunkZ)
        {
            ushort chunkPos = (ushort)(chunkY * ChunkMesh.CHUNK_AREA + chunkX * ChunkMesh.CHUNK_SIZE + chunkZ);
            Position(chunkPos);
        }

        public void Dispose()
        {
            _stagingBuffer.Dispose();
        }

    }
}
