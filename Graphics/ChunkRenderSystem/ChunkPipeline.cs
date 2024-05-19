using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Vulkano.Engine;

namespace Vulkano.Graphics.ChunkRenderSystem
{
    internal class ChunkPipeline : VPipeline
    {

        public ChunkPipeline(VulkanEngine engine, Vk vk)
            : base(engine.Swapchain.RenderPass, engine, vk) { }

        protected override CullModeFlags CullMode => CullModeFlags.BackBit;

        protected override VertexInputAttributeDescription[] GetVertexDescriptions(out VertexInputBindingDescription? bindingDescription)
        {
            bindingDescription = null;
            return Array.Empty<VertexInputAttributeDescription>();
        }

        protected override PushConstantRange[] GetPushConstantRanges()
        {
            return new PushConstantRange[]
            {
                new PushConstantRange(ShaderStageFlags.VertexBit, 0, (uint)Marshal.SizeOf<ChunkPushConstant>())
            };
        }

        protected override void LoadShaderModules()
        {
            LoadShaderModule("./Graphics/Shaders/chunkVertexShader.spv", ShaderStageFlags.VertexBit);
            LoadShaderModule("./Graphics/Shaders/chunkFragmentShader.spv", ShaderStageFlags.FragmentBit);
        }

        protected unsafe override void RegisterDescriptors()
        {
            RegisterDescriptor(0, false,
                new DescriptorSetLayoutBinding(
                    binding: 0,
                    descriptorType: DescriptorType.StorageBuffer,
                    descriptorCount: 1, 
                    stageFlags: ShaderStageFlags.VertexBit,
                    pImmutableSamplers: null
                )
            );
            RegisterDescriptor(1, false,
                new DescriptorSetLayoutBinding(
                    binding: 0,
                    descriptorType: DescriptorType.CombinedImageSampler,
                    descriptorCount: 1,
                    stageFlags: ShaderStageFlags.FragmentBit,
                    pImmutableSamplers: null
                )
            );
        }

    }
}
