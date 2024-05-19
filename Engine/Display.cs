using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Vulkano.Engine
{
    internal class Display : IDisposable
    {

        private readonly IWindow _window;

        public uint Width => (uint)_window.Size.X;

        public uint Height => (uint)_window.Size.Y;

        public float AspectRatio => (float)Width / (float)Height;

        public string Title => _window.Title;

        public Extent2D FramebufferExtent => new((uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);

        public Action<uint, uint>? OnResize { get; set; }

        public IInputContext? InputContext { get; private set; }

        public Display(int width, int height, string title)
        {
            var opts = WindowOptions.DefaultVulkan;
            opts.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
            opts.Title = title;
            opts.WindowBorder = WindowBorder.Resizable;
            opts.UpdatesPerSecond = 60.0;
            opts.FramesPerSecond = -1.0;
            _window = Window.Create(opts);
        }

        private void OnLoad(Action init)
        {
            _window.Center();
            InputContext = _window.CreateInput();
            foreach (var keyboard in InputContext.Keyboards)
            {
                keyboard.KeyDown += (keyboard, key, scancode) =>
                {
                    if (key == Key.Escape)
                    {
                        _window.Close();
                    }
                };
            }
            init.Invoke();
        }

        public void Run(Action init, Action<double> update, Action<double> render, Action cleanUp)
        {
            _window.Load += () => OnLoad(init);
            _window.Update += update;
            _window.Render += render;
            _window.Closing += cleanUp;
            _window.Resize += size => OnResize?.Invoke((uint)size.X, (uint)size.Y);
            _window.Run();
        }

        public unsafe string[] GetRequiredInstanceExtensions()
        {
            return SilkMarshal.PtrToStringArray((nint)_window.VkSurface!.GetRequiredExtensions(out uint count), (int)count);
        }

        public unsafe SurfaceKHR CreateSurface(Instance instance)
        {
            return _window.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        }

        public void Dispose()
        {
            InputContext!.Dispose();
            _window.Dispose();
        }

    }
}
