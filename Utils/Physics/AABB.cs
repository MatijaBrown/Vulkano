using System.Numerics;

namespace Vulkano.Utils.Physics
{
    internal readonly struct AABB
    {

        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public AABB(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
            : this(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)) { }

        public AABB Expand(Vector3 direction)
        {
            Vector3 min = Min;
            Vector3 max = Max;
            if (direction.X < 0.0f)
            {
                min.X += direction.X;
            }
            else if (direction.X > 0.0f)
            {
                max.X += direction.X;
            }
            if (direction.Y < 0.0f)
            {
                min.Y += direction.Y;
            }
            else if (direction.Y > 0.0f)
            {
                max.Y += direction.Y;
            }
            if (direction.Z < 0.0f)
            {
                min.Z += direction.Z;
            }
            else if (direction.Z > 0.0f)
            {
                max.Z += direction.Z;
            }
            return new AABB(min, max);
        }

        public AABB Grow(Vector3 amount)
        {
            Vector3 min = Min - amount;
            Vector3 max = Max + amount;
            return new AABB(min, max);
        }

        public float ClipXCollide(AABB other, float dx)
        {
            if (!CanCollide(Min.Z, Max.Z, Min.Y, Max.Y, other.Min.Z, other.Max.Z, other.Min.Y, other.Max.Y))
            {
                return dx;
            }
            float max;
            if (dx > 0.0f)
            {
                if (other.Max.X <= Min.X && (max = Min.X - other.Max.X) < dx)
                {
                    dx = max;
                }
            }
            else if (dx < 0.0f)
            {
                if (other.Min.X >= Max.X && (max = Max.X - other.Min.X) > dx)
                {
                    dx = max;
                }
            }
            return dx;
        }

        public float ClipYCollide(AABB other, float dy)
        {
            if (!CanCollide(Min.X, Max.X, Min.Z, Max.Z, other.Min.X, other.Max.X, other.Min.Z, other.Max.Z))
            {
                return dy;
            }
            float max;
            if (dy > 0.0f)
            {
                if (other.Max.Y <= Min.Y && (max = Min.Y - other.Max.Y) < dy)
                {
                    dy = max;
                }
            }
            else if (dy < 0.0f)
            {
                if (other.Min.Y >= Max.Y && (max = Max.Y - other.Min.Y) > dy)
                {
                    dy = max;
                }
            }
            return dy;
        }

        public float ClipZCollide(AABB other, float dz)
        {
            if (!CanCollide(Min.X, Max.X, Min.Y, Max.Y, other.Min.X, other.Max.X, other.Min.Y, other.Max.Y))
            {
                return dz;
            }
            float max;
            if (dz > 0.0f)
            {
                if (other.Max.Z <= Min.Z && (max = Min.Z - other.Max.Z) < dz)
                {
                    dz = max;
                }
            }
            else if (dz < 0.0f)
            {
                if (other.Min.Z >= Max.Z && (max = Max.Z - other.Min.Z) > dz)
                {
                    dz = max;
                }
            }
            return dz;
        }

        public bool Intersects(AABB other)
        {
            return CanCollide(Min.X, Max.X, Min.Y, Max.Y, other.Min.X, other.Max.X, other.Min.Y, other.Max.Y) &&
                CanCollide(Min.X, Max.X, Min.Z, Max.Z, other.Min.X, other.Max.X, other.Min.Z, other.Max.Z) &&
                CanCollide(Min.Z, Max.Z, Min.Y, Max.Y, other.Min.Z, other.Max.Z, other.Min.Y, other.Max.Y);
        }

        public AABB Move(Vector3 moveDistance)
        {
            return new AABB(Min + moveDistance, Max + moveDistance);
        }

        private static bool CanCollide(float a0, float b0, float c0, float d0, float a1, float b1, float c1, float d1)
        {
            if (b0 <= a1 || a0 >= b1)
            {
                return false;
            }
            if (d0 <= c1 || c0 >= d1)
            {
                return false;
            }
            return true;
        }

    }
}
