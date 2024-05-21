using Silk.NET.Input;
using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.Graphics.SelectionRenderSystem;
using Vulkano.Utils;
using Vulkano.Utils.Maths;
using Vulkano.Utils.Physics;

namespace Vulkano.Entities
{
    internal class Player : IComparer<CollisionResolution>, IDisposable
    {

        public const float GROUND_SPEED = 1.2f;
        public const float AIR_SPEED = GROUND_SPEED / 4.0f;
        public const float GRAVITY = 32.0f;
        public const float JUMP_POWER = 15.0f;

        public const float WIDTH = 0.6f;
        public const float HEIGHT = 1.8f;

        public const float REACH = 4.0f;
        public const float MOUSE_SENSITIVITY = 0.15f;

        private readonly IInputContext _inputContext;
        private readonly World.World _world;

        private Vector2 _mousePosition = Vector2.Zero;
        private Vector2 _lastMousePosition = Vector2.Zero;

        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;

        private Vector3 _inputDirection = Vector3.Zero;
        private Vector3 _velocity = Vector3.Zero;

        private bool _isInAir = true;

        public Vector3 Eyes => Position + 1.62f * Vector3.UnitY;

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
        }

        private void MouseInput()
        {
            Vector2 mouseDelta = _mousePosition - _lastMousePosition;
            _rotationX += mouseDelta.Y * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            _rotationX = MathF.Max(MathF.Min(_rotationX, MathF.PI / 2.0f - 0.1f), -MathF.PI / 2.0f + 0.1f);
            _rotationY += mouseDelta.X * MOUSE_SENSITIVITY * MathF.PI / 180.0f;
            Facing = new Vector3(MathF.Cos(_rotationY) * MathF.Cos(_rotationX), -MathF.Sin(_rotationX), MathF.Sin(_rotationY) * MathF.Cos(_rotationX));
        }

        private void CalculateVelocity(float speed)
        {
            float v = _inputDirection.LengthSquared();
            if (v < 0.01f)
            {
                return;
            }
            v = speed / MathF.Sqrt(v);
            
            Vector3 forward = Vector3.Normalize(new Vector3(Facing.X, 0.0f, Facing.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            _velocity += v * (_inputDirection.Z * forward + _inputDirection.X * right /*+ _inputDirection.Y * Vector3.UnitY*/);
        }

        private static CollisionResolution MoveX(Cubeoid player, Cubeoid block, float dx)
        {
            if (block.XProjectionIntersects(player))
            {
                if (dx > +0.0001f)
                {
                    float distance = block.Min.X - player.Max.X;
                    if (distance <= dx)
                    {
                        return new CollisionResolution(Vector3.UnitX, distance - 0.001f);
                    }
                }
                else if (dx < -0.0001f)
                {
                    float distance = block.Max.X - player.Min.X; ;
                    if (distance >= dx)
                    {
                        return new CollisionResolution(Vector3.UnitX, distance + 0.001f);
                    }
                }
            }
            return new CollisionResolution(Vector3.Zero, float.PositiveInfinity);
        }

        private static CollisionResolution MoveY(Cubeoid player, Cubeoid block, float dy)
        {
            if (block.YProjectionIntersects(player))
            {
                if (dy > +0.0001f)
                {
                    float distance = block.Min.Y - player.Max.Y;
                    if (distance <= dy)
                    {
                        return new CollisionResolution(Vector3.UnitY, distance - 0.001f);
                    }
                }
                else if (dy < -0.0001f)
                {
                    float distance = block.Max.Y - player.Min.Y;
                    if (distance >= dy)
                    {
                        return new CollisionResolution(Vector3.UnitY, distance + 0.001f);
                    }
                }
            }
            return new CollisionResolution(Vector3.Zero, float.PositiveInfinity);
        }

        private static CollisionResolution MoveZ(Cubeoid player, Cubeoid block, float dz)
        {
            if (block.ZProjectionIntersects(player))
            {
                if (dz > +0.0001f)
                {
                    float distance = block.Min.Z - player.Max.Z;
                    if (distance <= dz)
                    {
                        return new CollisionResolution(Vector3.UnitZ, distance - 0.001f);
                    }
                }
                else if (dz < -0.0001f)
                {
                    float distance = block.Max.Z - player.Min.Z;
                    if (distance >= dz)
                    {
                        return new CollisionResolution(Vector3.UnitZ, distance + 0.001f);
                    }
                }
            }
            return new CollisionResolution(Vector3.Zero, float.PositiveInfinity);
        }

        public int Compare(CollisionResolution a, CollisionResolution b)
        {
            if (MathF.Abs(a.Scale) > MathF.Abs(b.Scale))
            {
                return 1;
            }
            else if (MathF.Abs(a.Scale) < MathF.Abs(b.Scale))
            {
                return -1;
            }
            return 0;
        }

        private void Move(Vector3 dist)
        {
            Vector3 p = Position + dist;
            var player = new Cubeoid(
                min: new Vector3(p.X - WIDTH / 2.0f, p.Y, p.Z - WIDTH / 2.0f),
                max: new Vector3(p.X + WIDTH / 2.0f, p.Y + HEIGHT, p.Z + WIDTH / 2.0f)
            );

            ISet<Cubeoid> candidates = new HashSet<Cubeoid>();
            for (int y = (int)MathF.Floor(player.Min.Y); y <= (int)MathF.Ceiling(player.Max.Y); y++)
            {
                for (int x = (int)MathF.Floor(player.Min.X); x <= (int)MathF.Ceiling(player.Max.X); x++)
                {
                    for (int z = (int)MathF.Floor(player.Min.Z); z <= (int)MathF.Ceiling(player.Max.Z); z++)
                    {
                        if (!_world.IsBlock((uint)x, (uint)y, (uint)z))
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

            foreach (Cubeoid cand in candidates)
            {
                p = Position + dist;
                player = new Cubeoid(
                    min: new Vector3(p.X - WIDTH / 2.0f, p.Y, p.Z - WIDTH / 2.0f),
                    max: new Vector3(p.X + WIDTH / 2.0f, p.Y + HEIGHT, p.Z + WIDTH / 2.0f)
                );
                if (player.XProjectionIntersects(cand) && player.YProjectionIntersects(cand) && player.ZProjectionIntersects(cand))
                {
                    var resolutions = new SortedSet<CollisionResolution>(this)
                    {
                        MoveX(player, cand, dist.X),
                        MoveY(player, cand, dist.Y),
                        MoveZ(player, cand, dist.Z)
                    };
                    CollisionResolution bestResolution = resolutions.Min;
                    if (!float.IsPositiveInfinity(resolutions.Min.Scale))
                    {
                        dist = new Vector3(bestResolution.Mask.X * dist.X, bestResolution.Mask.Y * dist.Y, bestResolution.Mask.Z * dist.Z);
                        if (bestResolution.Axis.X != 0.0f)
                        {
                            _velocity.X = 0.0f;
                        }
                        else if (bestResolution.Axis.Y != 0.0f)
                        {
                            _velocity.Y = 0.0f;
                        }
                        else if (bestResolution.Axis.Z != 0.0f)
                        {
                            _velocity.Z = 0.0f;
                        }
                    }
                }
            }

            if (MathF.Abs(dist.Y) > 0.0001f)
            {
                _isInAir = true;
            }
            else
            {
                _isInAir = false;
            }

            Position += dist;
        }

        float t = 1 / 200.0f;
        float acc = 0.0f;

        public void Update(float delta)
        {
            MouseInput();
            acc += delta;
            while (acc >= t)
            {
                CalculateVelocity(_isInAir ? AIR_SPEED : GROUND_SPEED);
                _velocity.Y -= 1.0f / 0.8f * GRAVITY * t;
                Move(_velocity * t);
                _velocity.X *= 0.91f;
                _velocity.Y *= 0.98f;
                _velocity.Z *= 0.91f;
                if (!_isInAir)
                {
                    _velocity.X *= 0.8f;
                    _velocity.Z *= 0.8f;
                }
                acc -= t;
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
                    _inputDirection.Z = 1.0f;
                    break;
                case Key.S:
                    _inputDirection.Z = -1.0f;
                    break;
                case Key.A:
                    _inputDirection.X = -1.0f;
                    break;
                case Key.D:
                    _inputDirection.X = 1.0f;
                    break;
                case Key.Space:
                    if (!_isInAir)
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
                    _inputDirection.Z =0;
                    break;
                case Key.S:
                    _inputDirection.Z = 0;
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
