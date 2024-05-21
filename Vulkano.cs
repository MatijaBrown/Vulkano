using Silk.NET.Input;
using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Entities;
using Vulkano.Graphics;
using Vulkano.Graphics.ChunkRenderSystem;
using Vulkano.Graphics.SelectionRenderSystem;
using Vulkano.Utils;
using Vulkano.Utils.Maths;

namespace Vulkano
{
    internal class Vulkano : IDisposable
    {

        private Vk? _vk;
        private VulkanEngine? _engine;

        private readonly Display _display;

        private readonly Utils.Timer _timer = new();

        private World.World? _world;
        private MasterRenderer? _renderer;

        private Player? _player;
        private Camera? _camera;

        private HitResult? _hitResult = null;

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

            _world = new World.World(256, 256, 64);
            _renderer.ChunkRenderer = new ChunkRenderer(_world, _engine, _vk);

            _camera = new Camera(70.0f * MathF.PI / 180.0f, 0.01f, 1000.0f, _display);
            _player = new Player(_world, _display.InputContext!);
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
                    if (key == Key.Enter)
                    {
                        _world.Save();
                    }
                };
            }

            _renderer.SelectionRenderer = new SelectionRenderer(_engine, _vk);

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

            _renderer?.Dispose();

            _engine!.Dispose();
            _vk!.Dispose();
        }

    }
}
