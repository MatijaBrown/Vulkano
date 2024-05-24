using System.Numerics;
using Vulkano.Physics;
using Vulkano.Utils.Maths;

namespace Vulkano.Entities.Creatures
{
    internal class Zombie : Creature
    {

        public const float GROUND_SPEED = 1.2f;
        public const float AIR_SPEED = GROUND_SPEED / 4.0f;
        public const float JUMP_POWER = 15.0f;

        public float AnimationTime { get; private set; }

        private float _rotateSpeed;

        public Zombie(World.World world)
            : base(new Cubeoid(-0.3f, 0.0f, -0.3f, 0.3f, 1.8f, 0.3f), world)
        {
            float x = (float)Random.Shared.NextDouble() * world.Width;
            float y = world.Height + 10.0f;
            float z = (float)Random.Shared.NextDouble() * world.Depth;
            Position = new Vector3(x, y, z);

            _rotateSpeed = ((float)Random.Shared.NextDouble() + 1.0f) * 0.01f;
            RotY = (float)Random.Shared.NextDouble() * MathF.PI * 2.0f;
        }

        public override void Tick(float dt, float time)
        {
            MoveInPlane(new Vector2(0, 1), IsInAir ? AIR_SPEED : GROUND_SPEED);
            Velocity.Y -= PhysicsEnvironment.GRAVITY * dt;
            Move(Velocity * dt);
            Velocity.X *= 0.91f;
            Velocity.Y *= 0.98f;
            Velocity.Z *= 0.91f;
            if (!IsInAir)
            {
                Velocity.X *= 0.7f;
                Velocity.Z *= 0.7f;
            }
            AnimationTime = time * 10.0f;
        }

        public override void Update(float delta)
        {
            if (!IsInAir && (Random.Shared.NextDouble() < 0.08))
            {
                Velocity.Y = JUMP_POWER;
            }

            RotY += _rotateSpeed;
            _rotateSpeed *= 0.99f;
            _rotateSpeed += (float)(Random.Shared.NextDouble() - Random.Shared.NextDouble()) * (float)(Random.Shared.NextDouble() * Random.Shared.NextDouble()) * 0.08f;
        }

    }
}
