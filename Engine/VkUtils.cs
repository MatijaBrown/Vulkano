using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal static class VkUtils
    {

        public static void AssertVk(Result result)
        {
            if (result != Result.Success)
            {
                throw new VulkanException("Vulkan call failed with result: " + result.ToString());
            }
        }

        public static unsafe bool CheckValidationLayers(string[] validationLayers, Vk vk)
        {
            uint layerCount = 0;
            AssertVk(vk.EnumerateInstanceLayerProperties(ref layerCount, null));
            LayerProperties* availableLayers = stackalloc LayerProperties[(int)layerCount];
            AssertVk(vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayers));

            foreach (string layerName in validationLayers)
            {
                bool layerFound = false;
                for (int i = 0; i < layerCount; i++)
                {
                    if (SilkMarshal.PtrToString((nint)availableLayers[i].LayerName) == layerName)
                    {
                        layerFound = true;
                        break;
                    }
                }
                if (!layerFound)
                {
                    return false;
                }
            }

            return true;
        }

        public static uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties, PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties)
        {
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if (((typeFilter & (1 << (int)i)) != 0) && physicalDeviceMemoryProperties.MemoryTypes[(int)i].PropertyFlags.HasFlag(properties))
                {
                    return i;
                }
            }
            throw new VulkanException("Failed to find suitable MemoryType!");
        }

        public static Format FindSupportedFormat(ImageTiling tiling, FormatFeatureFlags features, PhysicalDevice physicalDevice, Vk vk, params Format[] candidates)
        {
            foreach (Format format in candidates)
            {
                FormatProperties props = vk.GetPhysicalDeviceFormatProperties(physicalDevice, format);
                if ((tiling == ImageTiling.Linear) && props.LinearTilingFeatures.HasFlag(features))
                {
                    return format;
                }
                else if ((tiling == ImageTiling.Optimal) && props.OptimalTilingFeatures.HasFlag(features))
                {
                    return format;
                }
            }
            throw new VulkanException("Failed to find any supported acceptable Format!");
        }

        public static bool HasStencilComponent(Format format)
        {
            return (format == Format.D32SfloatS8Uint) || (format == Format.D24UnormS8Uint);
        }

    }
}
