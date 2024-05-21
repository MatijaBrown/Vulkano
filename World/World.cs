using System.IO.Compression;
using System.Security.Cryptography;

namespace Vulkano.World
{
    internal class World
    {

        private readonly IList<IWorldChangeListener> _changeListeners = new List<IWorldChangeListener>();

        private readonly byte[] _blocks;
        private readonly uint[] _lightDepths;

        public uint Width { get; }

        public uint Depth { get; }

        public uint Height { get; }

        public World(uint width, uint depth, uint height)
        {
            Width = width;
            Depth = depth;
            Height = height;

            _blocks = new byte[Width * Height * Depth];
            _lightDepths = new uint[Width * Depth];

            Load();
        }

        public void Load()
        {
            if (File.Exists("./world.sav"))
            {
                using FileStream fileStream = File.OpenRead("./world.sav");
                var zipStream = new GZipStream(fileStream, CompressionMode.Decompress, false);
                _ = zipStream.Read(_blocks);
                zipStream.Close();
                zipStream.Dispose();
                Console.WriteLine("World loaded from world.sav");
                CalcLightDepths(0, 0, Width, Depth);
            }
            else
            {
                Regenerate();
            }
        }

        public void Save()
        {
            using FileStream fileStream = File.Create("./world.sav");
            var zipStream = new GZipStream(fileStream, CompressionMode.Compress, false);
            zipStream.Write(_blocks);
            zipStream.Close();
            zipStream.Dispose();
            Console.WriteLine("World saved to world.sav");
        }

        public void Regenerate()
        {
            for (uint y = 0; y < Height; y++)
            {
                for (uint x = 0; x < Width; x++)
                {
                    for (uint z = 0; z < Depth; z++)
                    {
                        uint i = y * Width * Depth + x * Depth + z;
                        byte block = 0;
                        if (y <= Height * 2 / 3)
                        {
                            block = 1;
                        }
                        _blocks[i] = block;
                    }
                }
            }
            CalcLightDepths(0, 0, Width, Depth);
        }

        public void CalcLightDepths(uint x0, uint z0, uint w, uint d)
        {
            for (uint x = x0; x < (x0 + w); x++)
            {
                for (uint z = z0; z < (z0 + d); z++)
                {
                    uint y;
                    uint oldDepth = _lightDepths[x * Depth + z];
                    for (y = Depth - 1; (y > 0) && IsTransparent(x, y, z); y--) ;
                    _lightDepths[x * Depth + z] = y;
                    if (oldDepth == y)
                    {
                        continue;
                    }
                    uint yMin = Math.Min(oldDepth, y);
                    uint yMax = Math.Max(oldDepth, y);
                    foreach (IWorldChangeListener listener in _changeListeners)
                    {
                        listener.LightColumnChanged(x, z, yMin, yMax);
                    }
                }
            }
        }

        public void AddChangeListener(IWorldChangeListener listener)
        {
            _changeListeners.Add(listener);
        }

        public void RemoveChangeListener(IWorldChangeListener listener)
        {
            _ = _changeListeners.Remove(listener);
        }

        public bool IsBlock(uint x, uint y, uint z)
        {
            if (x >= Width || y >= Height || z >= Depth)
            {
                return false;
            }
            return _blocks[y * Width * Depth + x * Depth + z] == 1;
        }

        public bool IsTransparent(uint x, uint y, uint z)
        {
            return !IsBlock(x, y, z);
        }

        public uint GetLightLevel(uint x, uint y, uint z)
        {
            if (x >= Width || y >= Height || z >= Depth)
            {
                return 1;
            }
            if (y < _lightDepths[x * Depth + z])
            {
                return 0;
            }
            return 1;
        }

        public void SetBlock(uint x, uint y, uint z, byte type)
        {
            if (x >= Width || y >= Height || z >= Depth)
            {
                return;
            }
            _blocks[y * Width * Depth + x * Depth + z] = type;
            CalcLightDepths(x, z, 1, 1);
            foreach (IWorldChangeListener listener in _changeListeners)
            {
                listener.BlockChanged(x, y, z);
            }
        }

    }
}
