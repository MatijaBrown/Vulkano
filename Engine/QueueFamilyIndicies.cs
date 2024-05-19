using Silk.NET.Vulkan;

namespace Vulkano.Engine
{

    internal readonly struct QueueFamilyIndicies
    {
        private static readonly float QUEUE_PRIORITIES = 1.0f;

        public readonly uint? GraphicsFamily;
        public readonly uint? PresentFamily;

        public bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;

        public QueueFamilyIndicies(uint? graphicsFamily, uint? presentFamily)
        {
            GraphicsFamily = graphicsFamily;
            PresentFamily = presentFamily;
        }

        private static void AddQueueFamily(uint queueFamily, Dictionary<uint, uint> queueFamilies, out uint index)
        {
            if (!queueFamilies.ContainsKey(queueFamily))
            {
                queueFamilies.Add(queueFamily, 0);
            }
            index = queueFamilies[queueFamily];
            queueFamilies[queueFamily] += 1;
        }

        public unsafe DeviceQueueCreateInfo[] GetQueueCreateInfos(out uint graphicsQueueIndex, out uint presentQueueIndex)
        {
            var queueFamilyCounts = new Dictionary<uint, uint>();
            AddQueueFamily(GraphicsFamily!.Value, queueFamilyCounts, out graphicsQueueIndex);
            AddQueueFamily(PresentFamily!.Value, queueFamilyCounts, out presentQueueIndex);

            var queueCreateInfos = new DeviceQueueCreateInfo[queueFamilyCounts.Count];
            int i = 0;

            fixed (float* pPriority = &QUEUE_PRIORITIES)
            {
                foreach (var kvp in queueFamilyCounts)
                {
                    queueCreateInfos[i++] = new DeviceQueueCreateInfo(
                        queueFamilyIndex: kvp.Key,
                        queueCount: kvp.Value,
                        pQueuePriorities: pPriority
                    );
                }
            }

            return queueCreateInfos;
        }

    }

}
