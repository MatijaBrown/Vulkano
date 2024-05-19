using Vulkano.Engine;

namespace Vulkano
{
    class Program
    {
        static void Main()
        {
            var display = new Display(1024, 768, "Vulkano");
            var vulkano = new Vulkano(display);
            display.Run(vulkano.Init, vulkano.Update, vulkano.Render, vulkano.Dispose);
            display.Dispose();
        }
    }
}