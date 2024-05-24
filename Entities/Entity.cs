using System.Numerics;
using Vulkano.Utils.Maths;

namespace Vulkano.Entities
{
    internal abstract class Entity
    {

        protected readonly World.World World;

        public Cubeoid Bounds { get; protected set; }

        public Vector3 Position { get; protected set; }

        public float RotX { get; protected set; } = 0.0f;

        public float RotY { get; protected set; } = 0.0f;

        public Vector3 Facing => Vector3.Normalize(new Vector3(MathF.Cos(RotY) * MathF.Cos(RotX), -MathF.Sin(RotX), MathF.Sin(RotY) * MathF.Cos(RotX)));

        public Transform Transform => Transform.Identity.PrependTranslation(Position).PrependRotation(Quaternion.CreateFromYawPitchRoll(-RotY + MathF.PI / 2.0f, RotX, 0.0f));

        protected Entity(Vector3 position, Cubeoid bounds, World.World world)
        {
            Position = position;
            Bounds = bounds;
            World = world;
        }

        protected Entity(Cubeoid bounds, World.World world)
            : this(Vector3.Zero, bounds, world) { }

        public abstract void Update(float delta);

    }
}
