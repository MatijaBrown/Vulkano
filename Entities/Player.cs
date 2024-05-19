using Silk.NET.Input;
using System.Numerics;
using Vulkano.Graphics.SelectionRenderSystem;
using Vulkano.Utils;
using Vulkano.Utils.Maths;
using Vulkano.Utils.Physics;

namespace Vulkano.Entities
{
    internal class Player : IDisposable
    {

        public const float GROUND_SPEED = 1.2f;
        public const float AIR_SPEED = GROUND_SPEED / 4.0f;
        public const float GRAVITY = 0.4f;
        public const float JUMP_POWER = 8.5f;

        public const float WIDTH = 0.3f;
        public const float HEIGHT = 1.8f;

        public const float REACH = 4.0f;
        public const float MOUSE_SENSITIVITY = 0.15f;

        private readonly IInputContext _inputContext;
        private readonly World.World _world;

        private Vector2 _mousePosition = Vector2.Zero;
        private Vector2 _lastMousePosition = Vector2.Zero;

        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;

        private Vector2 _inputDirection = Vector2.Zero;
        private Vector3 _velocity = Vector3.Zero;

        private AABB _boundingBox;
        private bool _isOnGround = false;

        public Vector3 Position { get; private set; }

        public Vector3 Facing { get; private set; }

        public Player(World.World world, IInputContext inputContext)
        {
            _world = world;
            _inputContext = inputContext;

            InitialiseInput();
            ResetPosition();
        }

        private void ResetPosition()
        {
            float x = (float)Random.Shared.NextDouble() * _world.Width;
            float y = _world.Height + 10.0f;
            float z = (float)Random.Shared.NextDouble() * _world.Depth;
            SetPosition(new Vector3(x, y, z));
        }

        private void SetPosition(Vector3 position)
        {
            Position = position;
            _boundingBox = new AABB(
                Position.X - WIDTH / 2.0f, Position.Y - HEIGHT / 2.0f, Position.Z - WIDTH / 2.0f,
                Position.X + WIDTH / 2.0f, Position.Y + HEIGHT / 2.0f, Position.Z + WIDTH / 2.0f
            );
        }

        private void MouseInput()
        {
            Vector2 mouseDelta = _mousePosition - _lastMousePosition;
            _rotationX += mouseDelta.Y * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            _rotationX = MathF.Max(MathF.Min(_rotationX, MathF.PI / 2.0f - 0.1f), -MathF.PI / 2.0f + 0.1f);
            _rotationY += mouseDelta.X * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            Facing = new Vector3(MathF.Cos(_rotationY) * MathF.Cos(_rotationX), -MathF.Sin(_rotationX), MathF.Sin(_rotationY) * MathF.Cos(_rotationX));
        }

        private void MoveRelative(float speed)
        {
            float v = _inputDirection.LengthSquared();
            if (v < 0.01f)
            {
                return;
            }
            v = speed / MathF.Sqrt(v);
            
            Vector3 forward = Vector3.Normalize(new Vector3(Facing.X, 0.0f, Facing.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            _velocity += v * (_inputDirection.Y * forward + _inputDirection.X * right);
        }

        private void Move(Vector3 d, float dt)
        {
            Vector3 initialVelocity = d;
            IReadOnlyList<AABB> worldBounds = _world.GetBlocksInRange(_boundingBox.Expand(d));
            for (int i = 0; i < worldBounds.Count; i++)
            {
                d.Y = worldBounds[i].ClipYCollide(_boundingBox, d.Y);
            }
            _boundingBox = _boundingBox.Move(new Vector3(0.0f, d.Y, 0.0f));
            for (int i = 0; i < worldBounds.Count; i++)
            {
                d.X = worldBounds[i].ClipXCollide(_boundingBox, d.X);
            }
            _boundingBox = _boundingBox.Move(new Vector3(d.X, 0.0f, 0.0f));
            for (int i = 0; i < worldBounds.Count; i++)
            {
                d.Z = worldBounds[i].ClipZCollide(_boundingBox, d.Z);
            }
            _boundingBox = _boundingBox.Move(new Vector3(0.0f, 0.0f, d.Z));

            if (d.X != initialVelocity.X)
            {
                _velocity.X = 0.0f;
            }
            if (d.Y != initialVelocity.Y)
            {
                _isOnGround = initialVelocity.Y < 0.0f;
                _velocity.Y = 0.0f;
            }
            if (d.Z != initialVelocity.Z)
            {
                _velocity.Z = 0.0f;
            }

            Vector3 p = Position;
            p.X = (_boundingBox.Min.X + _boundingBox.Max.X) / 2.0f;
            p.Y = _boundingBox.Min.Y + 1.62f;
            p.Z = (_boundingBox.Min.Z + _boundingBox.Max.Z) / 2.0f;
            Position = p;
        }

        public void Update(float delta)
        {
            MouseInput();
            MoveRelative(_isOnGround ? GROUND_SPEED : AIR_SPEED);
            _velocity.Y -= GRAVITY;
            Move(_velocity * delta, delta);

            _velocity.X *= 0.91f;
            _velocity.Y *= 0.98f;
            _velocity.Z *= 0.91f;

            if (_isOnGround)
            {
                _velocity.X *= 0.8f;
                _velocity.Z *= 0.8f;
            }

            _lastMousePosition = _mousePosition;
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
                    if (_isOnGround)
                    {
                        _velocity.Y = JUMP_POWER;
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
                    _inputDirection.Y =0;
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
