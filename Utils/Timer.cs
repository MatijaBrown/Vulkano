using System.Diagnostics;

namespace Vulkano.Utils
{
    internal class Timer
    {

        private readonly Stopwatch _stopwatch;

        private uint _updates = 0;
        private uint _frames = 0;

        public Timer()
        {
            _stopwatch = new();
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Reset()
        {
            _updates = 0;
            _frames = 0;
            _stopwatch.Reset();
        }

        public void Update()
        {
            _updates++;
            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine($"Updates: {_updates} | Frames: {_frames}");
                _updates = 0;
                _frames = 0;
                _stopwatch.Restart();
            }
        }

        public void Frame()
        {
            _frames++;
        }

    }
}
