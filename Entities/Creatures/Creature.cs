using System.Numerics;
using Vulkano.Physics;
using Vulkano.Utils.Maths;

namespace Vulkano.Entities.Creatures
{
    internal abstract class Creature : Entity, IPhysicsObject
    {

        protected Vector3 Velocity = Vector3.Zero;

        protected bool IsInAir { get; private set; } = false;

        protected Creature(Vector3 position, Cubeoid bounds, World.World world)
            : base(position, bounds, world) { }

        protected Creature(Cubeoid bounds, World.World world)
            : base(bounds, world) { }

        public abstract void Tick(float dt, float time);

        protected void MoveInPlane(Vector2 direction, float speed)
        {
            float v = direction.LengthSquared();
            if (v < 0.01f)
            {
                return;
            }
            v = speed / MathF.Sqrt(v);

            Vector3 forward = Vector3.Normalize(new Vector3(Facing.X, 0.0f, Facing.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            Velocity += v * (direction.Y * forward + direction.X * right);
        }

        private ISet<Cubeoid> BroadPhaseCollide(Vector3 delta)
        {
            Vector3 newPosition = Position + delta;
            Cubeoid bounds = Bounds.At(newPosition);

            var candidates = new HashSet<Cubeoid>();
            for (int y = (int)MathF.Floor(bounds.Min.Y); y <= (int)MathF.Ceiling(bounds.Max.Y); y++)
            {
                for (int x = (int)MathF.Floor(bounds.Min.X); x <= (int)MathF.Ceiling(bounds.Max.X); x++)
                {
                    for (int z = (int)MathF.Floor(bounds.Min.Z); z <= (int)MathF.Ceiling(bounds.Max.Z); z++)
                    {
                        if (!World.IsBlock((uint)x, (uint)y, (uint)z))
                        {
                            continue;
                        }
                        candidates.Add(new Cubeoid(
                                minX: x,
                                minY: y,
                                minZ: z,

                                maxX: x + 1.0f,
                                maxY: y + 1.0f,
                                maxZ: z + 1.0f
                            )
                        );
                    }
                }
            }

            return candidates;
        }

        private Vector3 NarrowPhaseCollide(Vector3 delta, ISet<Cubeoid> candidates)
        {
            foreach (Cubeoid candidate in candidates)
            {
                Vector3 newPosition = Position + delta;
                Cubeoid bounds = Bounds.At(newPosition);

                if (bounds.XProjectionIntersects(candidate) && bounds.YProjectionIntersects(candidate) && bounds.ZProjectionIntersects(candidate))
                {
                    float resolutionX = MathF.Abs(CollideX(bounds, candidate, delta.X));
                    float resolutionY = MathF.Abs(CollideY(bounds, candidate, delta.Y));
                    float resolutionZ = MathF.Abs(CollideZ(bounds, candidate, delta.Z));

                    if ((resolutionX <= resolutionY) && (resolutionX <= resolutionZ))
                    {
                        delta = new Vector3(0.0f, delta.Y, delta.Z);
                        Velocity.X = 0.0f;
                    }
                    if ((resolutionY <= resolutionX) && (resolutionY <= resolutionZ))
                    {
                        delta = new Vector3(delta.X, 0.0f, delta.Z);
                        Velocity.Y = 0.0f;
                    }
                    if ((resolutionZ <= resolutionX) && (resolutionZ <= resolutionY))
                    {
                        delta = new Vector3(delta.X, delta.Y, 0.0f);
                        Velocity.Z = 0.0f;
                    }
                }
            }
            return delta;
        }

        public void Move(Vector3 delta)
        {
            ISet<Cubeoid> candidates = BroadPhaseCollide(delta);
            delta = NarrowPhaseCollide(delta, candidates);

            if (MathF.Abs(delta.Y) > 0.0001f)
            {
                IsInAir = true;
            }
            else
            {
                IsInAir = false;
            }

            Position += delta;
        }

        private static float CollideX(Cubeoid @this, Cubeoid other, float dx)
        {
            if (other.XProjectionIntersects(@this))
            {
                if (dx > +0.0001f)
                {
                    float distance = other.Min.X - @this.Max.X;
                    if (distance <= dx)
                    {
                        return distance - 0.01f;
                    }
                }
                else if (dx < -0.0001f)
                {
                    float distance = other.Max.X - @this.Min.X; ;
                    if (distance >= dx)
                    {
                        return distance + 0.01f;
                    }
                }
            }
            return float.PositiveInfinity;
        }

        private static float CollideY(Cubeoid @this, Cubeoid other, float dy)
        {
            if (other.YProjectionIntersects(@this))
            {
                if (dy > +0.0001f)
                {
                    float distance = other.Min.Y - @this.Max.Y;
                    if (distance <= dy)
                    {
                        return distance - 0.01f;
                    }
                }
                else if (dy < -0.0001f)
                {
                    float distance = other.Max.Y - @this.Min.Y;
                    if (distance >= dy)
                    {
                        return distance + 0.01f;
                    }
                }
            }
            return float.PositiveInfinity;
        }

        private static float CollideZ(Cubeoid @this, Cubeoid other, float dz)
        {
            if (other.ZProjectionIntersects(@this))
            {
                if (dz > +0.0001f)
                {
                    float distance = other.Min.Z - @this.Max.Z;
                    if (distance <= dz)
                    {
                        return distance - 0.01f;
                    }
                }
                else if (dz < -0.0001f)
                {
                    float distance = other.Max.Z - @this.Min.Z;
                    if (distance >= dz)
                    {
                        return distance + 0.01f;
                    }
                }
            }
            return float.PositiveInfinity;
        }

    }
}
