using System.Numerics;

namespace Vulkano.Utils.Maths
{
    internal readonly struct Cubeoid
    {

        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public Vector3 Size => Max - Min;

        public Vector3 Center => Min + Size / 2.0f;

        public Cubeoid(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Cubeoid(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
            : this(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)) { }

        public bool XProjectionIntersects(Cubeoid other)
        {
            if (Min.Y >= other.Max.Y || Max.Y <= other.Min.Y)
            {
                return false;
            }
            if (Min.Z >= other.Max.Z || Max.Z <= other.Min.Z)
            {
                return false;
            }
            return true;
        }

        public bool YProjectionIntersects(Cubeoid other)
        {
            if (Min.X >= other.Max.X || Max.X <= other.Min.X)
            {
                return false;
            }
            if (Min.Z >= other.Max.Z || Max.Z <= other.Min.Z)
            {
                return false;
            }
            return true;
        }

        public bool ZProjectionIntersects(Cubeoid other)
        {
            if (Min.X >= other.Max.X || Max.X <= other.Min.X)
            {
                return false;
            }
            if (Min.Y >= other.Max.Y || Max.Y <= other.Min.Y)
            {
                return false;
            }
            return true;
        }

    }
}
