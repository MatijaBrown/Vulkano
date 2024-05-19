using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.NV;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal class VulkanEngine : IDisposable
    {

        private static readonly string[] VALIDATION_LAYERS =
        {
            "VK_LAYER_KHRONOS_validation"
        };

        private static readonly string[] DEVICE_EXTENSIONS = 
        {
            KhrSwapchain.ExtensionName,
            KhrPushDescriptor.ExtensionName,
            NVMeshShader.ExtensionName
        };

#if DEBUG
        public const bool ENABLE_VALIDATION_LAYERS = true;
#else
        public const bool ENABLE_VALIDATION_LAYERS = false;
#endif

        private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity,
            DebugUtilsMessageTypeFlagsEXT messageType, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            Console.Error.WriteLine("Validation Layer: " + SilkMarshal.PtrToString((nint)pCallbackData->PMessage));
            return 0;
        }

        private readonly Vk _vk;

        private readonly Display _display;

        private readonly ExtDebugUtils? _extDebugUtils;
        private readonly DebugUtilsMessengerEXT? _debugMessenger;

        private readonly PhysicalDeviceProperties _physicalDeviceProperties;
        private readonly PhysicalDeviceFeatures _physicalDeviceFeatures;
        private readonly PhysicalDeviceMemoryProperties _physicalDeviceMemoryProperties;
        private readonly QueueFamilyIndicies _queueFamilyIndices;

        private readonly Queue _graphicsQueue;
        private readonly Queue _presentQueue;

        private readonly KhrSurface _extSurface;
        private readonly NVMeshShader _extMeshShader;

        private readonly CommandPool _commandPool;

        public Instance Instance { get; }

        public QueueFamilyIndicies QueueFamilyIndicies => _queueFamilyIndices;

        public PhysicalDeviceProperties PhysicalDeviceProperties => _physicalDeviceProperties;

        public PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties => _physicalDeviceMemoryProperties;

        public PhysicalDevice PhysicalDevice { get; }

        public Device Device { get; }

        public Queue GraphicsQueue => _graphicsQueue;

        public Queue PresentQueue => _presentQueue;

        public SurfaceKHR Surface { get; }

        public DescriptorPool DescriptorPool { get; }

        public NVMeshShader ExtMeshShader => _extMeshShader;

        public KhrSurface ExtSurface => _extSurface;

        public VSwapchain Swapchain { get; }

        public KhrPushDescriptor ExtPushDescriptor { get; }

        public VulkanEngine(Display display, Vk vk)
        {
            _vk = vk;
            _display = display;

            Instance = CreateInstance();
            _debugMessenger = SetupDebugMessenger(out _extDebugUtils);
            Surface = CreateSurface(out _extSurface);
            PhysicalDevice = PickPhysicalDevice(out _queueFamilyIndices, out _physicalDeviceProperties, out _physicalDeviceFeatures, out _physicalDeviceMemoryProperties);
            Device = CreateLogicalDevice(out _graphicsQueue, out _presentQueue, out _extMeshShader);
            _commandPool = CreateCommandPool();
            Swapchain = new VSwapchain(_display, this, _vk);
            ExtPushDescriptor = GetPushDescriptorExtension();
            DescriptorPool = CreateDescriptorPool();
        }

        private unsafe string[] GetRequiredExtensions()
        {
            string[] windowExtensions = _display.GetRequiredInstanceExtensions();
            var extensions = new HashSet<string>(windowExtensions);

            if (ENABLE_VALIDATION_LAYERS)
            {
                extensions.Add(ExtDebugUtils.ExtensionName);
            }

            return extensions.ToArray();
        }

        private unsafe Instance CreateInstance()
        {
            if (ENABLE_VALIDATION_LAYERS && !VkUtils.CheckValidationLayers(VALIDATION_LAYERS, _vk))
            {
                throw new VulkanException("ValidationLayers requested, but not available!");
            }

            var appInfo = new ApplicationInfo(
                pApplicationName: (byte*)SilkMarshal.StringToPtr(_display.Title),
                applicationVersion: Vk.MakeVersion(0, 0, 1),
                pEngineName: (byte*)SilkMarshal.StringToPtr(_display.Title + " Engine"),
                engineVersion: Vk.MakeVersion(0, 0, 1),
                apiVersion: Vk.Version12
            );

            string[] extensions = GetRequiredExtensions();

            var createInfo = new InstanceCreateInfo(
                pApplicationInfo: &appInfo,

                enabledExtensionCount: (uint)extensions.Length,
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(extensions)
            );

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = PopulateDebugUtilsMessengerCreateInfo();
            if (ENABLE_VALIDATION_LAYERS)
            {
                createInfo.EnabledLayerCount = (uint)VALIDATION_LAYERS.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(VALIDATION_LAYERS);

                createInfo.PNext = &debugCreateInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PpEnabledLayerNames = null;
                createInfo.PNext = null;
            }

            if (_vk.CreateInstance(createInfo, null, out Instance instance) != Result.Success)
            {
                throw new VulkanException("Failed to create Instance!");
            }

            return instance;
        }

        private static unsafe DebugUtilsMessengerCreateInfoEXT PopulateDebugUtilsMessengerCreateInfo()
        {
            return new DebugUtilsMessengerCreateInfoEXT(
                messageSeverity: DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                messageType: DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                pfnUserCallback: (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
                pUserData: null
            );
        }

        private unsafe DebugUtilsMessengerEXT? SetupDebugMessenger(out ExtDebugUtils? extDebugUtils)
        {
            if (!ENABLE_VALIDATION_LAYERS)
            {
                extDebugUtils = null;
                return null;
            }

            if (!_vk.TryGetInstanceExtension(Instance, out extDebugUtils))
            {
                throw new VulkanException("Failed to get ExtDebugUtils-Extension!");
            }

            DebugUtilsMessengerCreateInfoEXT createInfo = PopulateDebugUtilsMessengerCreateInfo();

            if (extDebugUtils!.CreateDebugUtilsMessenger(Instance, createInfo, null, out DebugUtilsMessengerEXT debugMessenger) != Result.Success)
            {
                throw new VulkanException("Failed to create DebugUtilsMessenger!");
            }

            return debugMessenger;
        }

        private unsafe QueueFamilyIndicies FindQueueFamilies(PhysicalDevice physicalDevice)
        {
            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, null);
            QueueFamilyProperties* queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
            _vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, queueFamilies);

            uint? graphicsFamily = null;
            uint? presentFamily = null;

            for (uint i = 0; i < queueFamilyCount; i++)
            {
                QueueFamilyProperties queueFamily = queueFamilies[i];
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    graphicsFamily = i;
                }

                VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, Surface, out Bool32 presentSupport));
                if (presentSupport)
                {
                    presentFamily = i;
                }
            }

            return new QueueFamilyIndicies(graphicsFamily, presentFamily);
        }

        private unsafe bool CheckDeviceExtensionSupport(PhysicalDevice device)
        {
            uint extensionCount = 0;
            VkUtils.AssertVk(_vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null));
            ExtensionProperties* availableExtensions = stackalloc ExtensionProperties[(int)extensionCount];
            VkUtils.AssertVk(_vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, availableExtensions));

            var requiredExtensions = new HashSet<string>(DEVICE_EXTENSIONS);

            for (int i = 0; i < extensionCount; i++)
            {
                string extension = SilkMarshal.PtrToString((nint)availableExtensions[i].ExtensionName)!;
                _ = requiredExtensions.Remove(extension);
            }

            return requiredExtensions.Count == 0;
        }

        private unsafe VSwapchain.SwapchainSupportDetails QuerySwapchainSupport(PhysicalDevice device)
        {
            VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfaceCapabilities(device, Surface, out SurfaceCapabilitiesKHR capabilities));

            uint formatCount = 0;
            VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, ref formatCount, null));
            var formats = new SurfaceFormatKHR[formatCount];
            VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, ref formatCount, out formats[0]));

            uint presentModeCount = 0;
            VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, ref presentModeCount, null));
            var presentModes = new PresentModeKHR[presentModeCount];
            VkUtils.AssertVk(_extSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, ref presentModeCount, out presentModes[0]));

            return new VSwapchain.SwapchainSupportDetails(capabilities, formats, presentModes);
        }

        public VSwapchain.SwapchainSupportDetails QuerySwapchainSupport() => QuerySwapchainSupport(PhysicalDevice);

        private unsafe int RatePhysicalDeviceSuitability(PhysicalDevice physicalDevice,
            out QueueFamilyIndicies queueFamilies, out PhysicalDeviceProperties deviceProperties, out PhysicalDeviceFeatures deviceFeatures)
        {
            deviceProperties = _vk.GetPhysicalDeviceProperties(physicalDevice);
            deviceFeatures = _vk.GetPhysicalDeviceFeatures(physicalDevice);
            FormatProperties formatProperties = _vk.GetPhysicalDeviceFormatProperties(physicalDevice, Format.R8G8B8A8Srgb);

            int score = 0;

            if (deviceProperties.DeviceType == PhysicalDeviceType.DiscreteGpu)
            {
                score += 1000;
            }

            score += (int)deviceProperties.Limits.MaxImageDimension2D;

            queueFamilies = FindQueueFamilies(physicalDevice);
            if (!queueFamilies.IsComplete)
            {
                return -1;
            }

            if (!CheckDeviceExtensionSupport(physicalDevice))
            {
                return -1;
            }

            VSwapchain.SwapchainSupportDetails swapchainDetails = QuerySwapchainSupport(physicalDevice);
            if (!swapchainDetails.IsAdequate)
            {
                return -1;
            }

            if (!deviceFeatures.SamplerAnisotropy)
            {
                return -1;
            }

            if (!deviceFeatures.GeometryShader)
            {
                return -1;
            }

            if (!formatProperties.OptimalTilingFeatures.HasFlag(FormatFeatureFlags.SampledImageFilterLinearBit))
            {
                return -1;
            }

            return score;
        }

        private unsafe PhysicalDevice PickPhysicalDevice(out QueueFamilyIndicies queueFamilyIndicies, out PhysicalDeviceProperties physicalDeviceProperties,
            out PhysicalDeviceFeatures physicalDeviceFeatures, out PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties)
        {
            queueFamilyIndicies = new();
            physicalDeviceProperties = new();
            physicalDeviceFeatures = new();
            physicalDeviceMemoryProperties = new();

            uint deviceCount = 0;
            VkUtils.AssertVk(_vk.EnumeratePhysicalDevices(Instance, ref deviceCount, null));
            PhysicalDevice* devices = stackalloc PhysicalDevice[(int)deviceCount];
            VkUtils.AssertVk(_vk.EnumeratePhysicalDevices(Instance, ref deviceCount, devices));

            PhysicalDevice? bestDevice = null;
            int bestScore = -1;

            for (uint i = 0; i < deviceCount; i++)
            {
                PhysicalDevice currentDevice = devices[i];
                int score = RatePhysicalDeviceSuitability(currentDevice,
                    out QueueFamilyIndicies queueFamilies, out PhysicalDeviceProperties properties, out PhysicalDeviceFeatures features);
                if ((score >= 0) && (score > bestScore))
                {
                    bestDevice = currentDevice;
                    bestScore = score;
                    queueFamilyIndicies = queueFamilies;
                    physicalDeviceProperties = properties;
                    physicalDeviceFeatures = features;
                    physicalDeviceMemoryProperties = _vk.GetPhysicalDeviceMemoryProperties(currentDevice);
                }
            }

            if (!bestDevice.HasValue)
            {
                throw new VulkanException("No suitable PhysicalDevice found!");
            }
            return bestDevice!.Value;
        }

        private unsafe Device CreateLogicalDevice(out Queue graphicsQueue, out Queue presentQueue, out NVMeshShader extMeshShader)
        {
            DeviceQueueCreateInfo[] queueCreateInfos = _queueFamilyIndices.GetQueueCreateInfos(out uint graphicsQueueIndex, out uint presentQueueIndex);
            var deviceFeatures = new PhysicalDeviceFeatures(
                samplerAnisotropy: true,
                shaderStorageBufferArrayDynamicIndexing: true,
                fillModeNonSolid: true
            );

            var createInfo = new DeviceCreateInfo(
                pEnabledFeatures: &deviceFeatures,

                enabledExtensionCount: (uint)DEVICE_EXTENSIONS.Length,
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(DEVICE_EXTENSIONS)
            );

            fixed (DeviceQueueCreateInfo* ptr = queueCreateInfos)
            {
                createInfo.QueueCreateInfoCount = (uint)queueCreateInfos.Length;
                createInfo.PQueueCreateInfos = ptr;
            }

            if (ENABLE_VALIDATION_LAYERS)
            {
                createInfo.EnabledLayerCount = (uint)VALIDATION_LAYERS.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(VALIDATION_LAYERS);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PpEnabledLayerNames = null;
            }

            if (_vk.CreateDevice(PhysicalDevice, createInfo, null, out Device device) != Result.Success)
            {
                throw new VulkanException("Failed to create logical Device!");
            }

            graphicsQueue = _vk.GetDeviceQueue(device, _queueFamilyIndices.GraphicsFamily!.Value, graphicsQueueIndex);
            presentQueue = _vk.GetDeviceQueue(device, _queueFamilyIndices.PresentFamily!.Value, presentQueueIndex);

            if (!_vk.TryGetDeviceExtension(Instance, device, out extMeshShader))
            {
                throw new VulkanException("Failed to get NVMeshShader-Extension!");
            }

            return device;
        }

        private unsafe SurfaceKHR CreateSurface(out KhrSurface extSurface)
        {
            if (!_vk.TryGetInstanceExtension(Instance, out extSurface))
            {
                throw new VulkanException("Failed to get KhrSurface-Extension!");
            }
            return _display.CreateSurface(Instance);
        }

        private unsafe CommandPool CreateCommandPool()
        {
            var createInfo = new CommandPoolCreateInfo(
                flags: CommandPoolCreateFlags.ResetCommandBufferBit,
                queueFamilyIndex: QueueFamilyIndicies.GraphicsFamily
            );

            if (_vk.CreateCommandPool(Device, createInfo, null, out CommandPool commandPool) != Result.Success)
            {
                throw new VulkanException("Failed to create CommandPool!");
            }

            return commandPool;
        }

        private KhrPushDescriptor GetPushDescriptorExtension()
        {
            if (!_vk.TryGetDeviceExtension(Instance, Device, out KhrPushDescriptor extPushDescriptor))
            {
                throw new VulkanException("Failed to get DeviceExtension \"" + KhrPushDescriptor.ExtensionName + "\"!");
            }
            return extPushDescriptor;
        }

        private unsafe DescriptorPool CreateDescriptorPool()
        {
            DescriptorPoolSize* sizes = stackalloc DescriptorPoolSize[2]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 10000),
                new DescriptorPoolSize(DescriptorType.StorageBuffer, 10000)
            };

            var createInfo = new DescriptorPoolCreateInfo(
                maxSets: 10000,
                poolSizeCount: 2,
                pPoolSizes: sizes
            );

            if (_vk.CreateDescriptorPool(Device, createInfo, null, out DescriptorPool pool) != Result.Success)
            {
                throw new VulkanException("Failed to create DescriptorPool!");
            }

            return pool;
        }

        public unsafe CommandBuffer[] AllocateCommandBuffers(uint count)
        {
            var commandBuffers = new CommandBuffer[count];

            var allocateInfo = new CommandBufferAllocateInfo(
                commandPool: _commandPool,
                level: CommandBufferLevel.Primary,
                commandBufferCount: count
            );

            if (_vk.AllocateCommandBuffers(Device, allocateInfo, out commandBuffers[0]) != Result.Success)
            {
                throw new VulkanException("Failed to allocate CommandBuffer(s)!");
            }

            return commandBuffers;
        }

        public unsafe void FreeCommandBuffers(params CommandBuffer[] commandBuffers)
        {
            _vk.FreeCommandBuffers(Device, _commandPool, commandBuffers);
        }

        public unsafe CommandBuffer BeginSingleUseCommandBuffer()
        {
            CommandBuffer commandBuffer = AllocateCommandBuffers(1)[0];

            var beginInfo = new CommandBufferBeginInfo(
                flags: CommandBufferUsageFlags.OneTimeSubmitBit
            );
            VkUtils.AssertVk(_vk.BeginCommandBuffer(commandBuffer, beginInfo));

            return commandBuffer;
        }

        public unsafe void EndSingleUseCommandBuffer(CommandBuffer commandBuffer, Queue queue)
        {
            VkUtils.AssertVk(_vk.EndCommandBuffer(commandBuffer));

            var submitInfo = new SubmitInfo(
                commandBufferCount: 1,
                pCommandBuffers: &commandBuffer
            );

            VkUtils.AssertVk(_vk.QueueSubmit(queue, 1, submitInfo, new Fence(handle: null)));
            VkUtils.AssertVk(_vk.QueueWaitIdle(queue));

            FreeCommandBuffers(commandBuffer);
        }

        public unsafe DescriptorSet AllocateDescriptor(DescriptorSetLayout layout)
        {
            var allocInfo = new DescriptorSetAllocateInfo(
                descriptorPool: DescriptorPool,
                descriptorSetCount: 1,
                pSetLayouts: &layout
            );
            if (_vk.AllocateDescriptorSets(Device, allocInfo, out DescriptorSet descriptorSet) != Result.Success)
            {
                throw new VulkanException("Failed to allocate DescriptorSet!");
            }
            return descriptorSet;
        }

        public unsafe void WriteDescriptorSet(DescriptorSet descriptorSet, uint binding, DescriptorBufferInfo bufferInfo, DescriptorType descriptorType = DescriptorType.UniformBuffer)
        {
            var setWrite = new WriteDescriptorSet(
                dstBinding: binding,
                dstSet: descriptorSet,
                descriptorCount: 1,
                descriptorType: descriptorType,
                pBufferInfo: &bufferInfo
            );

            _vk.UpdateDescriptorSets(Device, 1, setWrite, 0, null);
        }

        public unsafe void WriteDescriptorSet(DescriptorSet descriptorSet, uint binding, DescriptorImageInfo imageInfo, DescriptorType descriptorType = DescriptorType.CombinedImageSampler)
        {
            var setWrite = new WriteDescriptorSet(
                dstBinding: binding,
                dstSet: descriptorSet,
                descriptorCount: 1,
                descriptorType: descriptorType,
                pImageInfo: &imageInfo
            );

            _vk.UpdateDescriptorSets(Device, 1, setWrite, 0, null);
        }

        public unsafe void Dispose()
        {
            _vk.DestroyDescriptorPool(Device, DescriptorPool, null);
            _vk.DestroyCommandPool(Device, _commandPool, null);
            Swapchain.Dispose();
            _extMeshShader.Dispose();
            _extSurface.DestroySurface(Instance, Surface, null);
            _extSurface.Dispose();
            _vk.DestroyDevice(Device, null);
            if (ENABLE_VALIDATION_LAYERS)
            {
                _extDebugUtils?.DestroyDebugUtilsMessenger(Instance, _debugMessenger!.Value, null);
                _extDebugUtils?.Dispose();
            }
            _vk.DestroyInstance(Instance, null);
        }

    }
}
