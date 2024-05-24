using Silk.NET.Input;
using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Entities.Creatures;
using Vulkano.Graphics;
using Vulkano.Graphics.ChunkRenderSystem;
using Vulkano.Graphics.ModelRenderSystem;
using Vulkano.Graphics.SelectionRenderSystem;
using Vulkano.Physics;
using Vulkano.Utils;
using Vulkano.Utils.Maths;

namespace Vulkano
{
    internal class Vulkano : IDisposable
    {

        private readonly Display _display;

        private readonly IList<Zombie> _zombies = new List<Zombie>();

        private readonly Utils.Timer _timer = new();
        private readonly PhysicsEnvironment _physicsEnvironment = new();

        private Vk? _vk;
        private VulkanEngine? _engine;

        private World.World? _world;
        private MasterRenderer? _renderer;

        private Player? _player;
        private Camera? _camera;

        private HitResult? _hitResult = null;

        private ZombieModel? _zombieModel;

        public Vulkano(Display display)
        {
            _display = display;
        }

        public void Init()
        {
            _vk = Vk.GetApi();
            _engine = new VulkanEngine(_display, _vk);

            _renderer = new MasterRenderer(_engine, _vk);
            _renderer.DebugLineRenderer = new DebugLineRenderer(_engine, _vk);
            _renderer.ModelRenderer = new ModelRenderer(_engine, _vk);

            _world = new World.World(256, 256, 64);
            _renderer.ChunkRenderer = new ChunkRenderer(_world, _engine, _vk);

            _camera = new Camera(70.0f * MathF.PI / 180.0f, 0.01f, 1000.0f, _display);
            _player = new Player(_world, _display.InputContext!);
            _physicsEnvironment.AddPhysicsObject(_player);
            _camera!.Intersection = (startPos, curPos) =>
            {
                if (curPos.X < 0 || curPos.Y < 0 || curPos.Z < 0)
                {
                    return false;
                }
                // It is not an intersection if we (the player) are (is) inside of a block.
                return _world!.IsBlock((uint)curPos.X, (uint)curPos.Y, (uint)curPos.Z) && !_world!.IsBlock((uint)startPos.X, (uint)startPos.Y, (uint)startPos.Z);
            };

            // Input
            foreach (IMouse mouse in _display.InputContext!.Mice)
            {
                mouse.Cursor.CursorMode = CursorMode.Disabled;
                mouse.MouseDown += MousePressed;
            }
            foreach (IKeyboard keyboard in _display.InputContext!.Keyboards)
            {
                keyboard.KeyDown += (_, key, sc) =>
                {
                    switch (key)
                    {
                        case Key.Enter:
                            _world.Save();
                            break;
                        case Key.G:
                            _world.Regenerate();
                            break;
                    }
                };
            }

            _renderer.SelectionRenderer = new SelectionRenderer(_engine, _vk);

            _zombieModel = new ZombieModel(_renderer!.ModelRenderer!, _engine, _vk);
            for (uint i = 0; i < 100; i++)
            {
                var zomb = new Zombie(_world!);
                _zombies.Add(zomb);
                _physicsEnvironment.AddPhysicsObject(zomb);
            }

            _timer.Start();
        }

        private void Pick()
        {
            if (_camera!.Pick(0.01f, 4.0f, out Vector3? position))
            {
                Vector3 otherPosition = position!.Value - Vector3.Normalize(_camera.Facing) * 0.01f;
                _hitResult = new HitResult()
                {
                    X = (uint)Math.Floor(position!.Value.X),
                    Y = (uint)Math.Floor(position!.Value.Y),
                    Z = (uint)Math.Floor(position!.Value.Z),
                    PreviousX = (uint)Math.Floor(otherPosition.X),
                    PreviousY = (uint)Math.Floor(otherPosition.Y),
                    PreviousZ = (uint)Math.Floor(otherPosition.Z)
                };
            }
            else
            {
                _hitResult = null;
            }
        }

        public void Update(double delta)
        {
            _player!.Update((float)delta);
            _camera!.MoveToPlayer(_player!);
            _camera!.Update();

            foreach (Zombie zomb in _zombies)
            {
                zomb.Update((float)delta);
            }

            _physicsEnvironment.Update((float)delta);

            Pick();

            _timer.Update();
        }

        private void MousePressed(IMouse _, MouseButton button)
        {
            if (!_hitResult.HasValue)
            {
                return;
            }

            if (button == MouseButton.Left)
            {
                uint x = _hitResult.Value.PreviousX;
                uint y = _hitResult.Value.PreviousY;
                uint z = _hitResult.Value.PreviousZ;
                _world?.SetBlock(x, y, z, 1);
            }
            else if (button == MouseButton.Right)
            {
                uint x = _hitResult.Value.X;
                uint y = _hitResult.Value.Y;
                uint z = _hitResult.Value.Z;
                _world?.SetBlock(x, y, z, 0);
            }
        }

        public void Render(double d)
        {
            foreach (Zombie zomb in _zombies)
            {
                _zombieModel!.RenderAt(zomb.Transform, zomb.AnimationTime);
            }

            if (_hitResult.HasValue)
            {
                _renderer!.SelectionRenderer!.RenderHit(_hitResult.Value);
            }

            _renderer!.Render(_camera!);

            _timer.Frame();
        }

        public void Dispose()
        {
            _world?.Save();

            _player!.Dispose();

            VkUtils.AssertVk(_vk!.DeviceWaitIdle(_engine!.Device));

            _zombieModel!.Dispose();

            _renderer?.Dispose();

            _engine!.Dispose();
            _vk!.Dispose();
        }

    }
}
