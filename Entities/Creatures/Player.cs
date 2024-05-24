using Silk.NET.Input;
using System.Numerics;
using Vulkano.Physics;
using Vulkano.Utils.Maths;

namespace Vulkano.Entities.Creatures
{
    internal class Player : Creature, IDisposable
    {

        public const float GROUND_SPEED = 1.2f;
        public const float AIR_SPEED = GROUND_SPEED / 4.0f;
        public const float JUMP_POWER = 15.0f;

        public const float WIDTH = 0.6f;
        public const float HEIGHT = 1.8f;

        public const float REACH = 4.0f;
        public const float MOUSE_SENSITIVITY = 0.15f;

        private readonly IInputContext _inputContext;

        private Vector2 _mousePosition = Vector2.Zero;
        private Vector2 _lastMousePosition = Vector2.Zero;

        private Vector2 _inputDirection = Vector2.Zero;
        private Vector3 _velocity = Vector3.Zero;

        public Vector3 Eyes => Position + 1.62f * Vector3.UnitY;

        public Player(World.World world, IInputContext inputContext)
            : base(new Cubeoid(-WIDTH / 2.0f, 0.0f, -WIDTH / 2.0f, WIDTH / 2.0f, HEIGHT, WIDTH / 2.0f), world)
        {
            _inputContext = inputContext;

            InitialiseInput();
            ResetPosition();
        }

        private void ResetPosition()
        {
            float x = 50.0f; // (float)Random.Shared.NextDouble() * _world.Width;
            float y = World.Height + 10.0f;
            float z = 50.0f; // (float)Random.Shared.NextDouble() * _world.Depth;
            SetPosition(new Vector3(x, y, z));
        }

        private void SetPosition(Vector3 position)
        {
            Position = position;
        }

        private void MouseInput()
        {
            Vector2 mouseDelta = _mousePosition - _lastMousePosition;
            RotX += mouseDelta.Y * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            RotX = MathF.Max(MathF.Min(RotX, MathF.PI / 2.0f - 0.1f), -MathF.PI / 2.0f + 0.1f);
            RotY += mouseDelta.X * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            _lastMousePosition = _mousePosition;
        }

        public override void Tick(float dt, float time)
        {
            MoveInPlane(_inputDirection, IsInAir ? AIR_SPEED : GROUND_SPEED);
            Velocity.Y -= PhysicsEnvironment.GRAVITY * dt;
            Move(Velocity * dt);
            Velocity.X *= 0.91f;
            Velocity.Y *= 0.98f;
            Velocity.Z *= 0.91f;
            if (!IsInAir)
            {
                Velocity.X *= 0.8f;
                Velocity.Z *= 0.8f;
            }
        }

        public override void Update(float delta)
        {
            MouseInput();
        }

        // Keyboard an mouse input

        private void InitialiseInput()
        {
            foreach (IKeyboard keyboard in _inputContext.Keyboards)
            {
                keyboard.KeyDown += KeyDown;
                keyboard.KeyUp += KeyUp;
            }
            foreach (IMouse mouse in _inputContext.Mice)
            {
                mouse.MouseMove += MouseMove;
            }
        }

        private void KeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            switch (key)
            {
                case Key.W:
                    _inputDirection.Y = 1.0f;
                    break;
                case Key.S:
                    _inputDirection.Y = -1.0f;
                    break;
                case Key.A:
                    _inputDirection.X = -1.0f;
                    break;
                case Key.D:
                    _inputDirection.X = 1.0f;
                    break;
                case Key.Space:
                    if (!IsInAir)
                    {
                        Velocity.Y = JUMP_POWER;
                    }
                    break;
                case Key.R:
                    ResetPosition();
                    break;
            }
        }

        private void KeyUp(IKeyboard keyboard, Key key, int scancode)
        {
            switch (key)
            {
                case Key.W:
                    _inputDirection.Y = 0;
                    break;
                case Key.S:
                    _inputDirection.Y = 0;
                    break;
                case Key.A:
                    _inputDirection.X = 0;
                    break;
                case Key.D:
                    _inputDirection.X = 0;
                    break;
            }
        }

        private void MouseMove(IMouse mouse, Vector2 delta)
        {
            _mousePosition = delta;
        }

        public void Dispose()
        {
            IReadOnlyList<IKeyboard> keyboards = _inputContext.Keyboards;
            foreach (var keyboard in keyboards)
            {
                keyboard.KeyDown -= KeyDown;
                keyboard.KeyUp -= KeyUp;
            }
            foreach (IMouse mouse in _inputContext.Mice)
            {
                mouse.MouseMove -= MouseMove;
            }
        }

    }
}
