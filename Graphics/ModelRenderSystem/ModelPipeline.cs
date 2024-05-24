using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Vulkano.Engine;

namespace Vulkano.Graphics.ModelRenderSystem
{
    internal class ModelPipeline : VPipeline
    {

        public ModelPipeline(VulkanEngine engine, Vk vk)
            : base(engine.Swapchain.RenderPass, engine, vk) { }

        protected override VertexInputAttributeDescription[] GetVertexDescriptions(out VertexInputBindingDescription? bindingDescription)
        {
            bindingDescription = new VertexInputBindingDescription(
                binding: 0,
                stride: (uint)Marshal.SizeOf<MeshVertex>(),
                inputRate: VertexInputRate.Vertex
            );

            return new VertexInputAttributeDescription[]
            {
                new(
                    location: 0,
                    binding: 0,
                    format: Format.R32G32B32Sfloat,
                    offset: (uint)Marshal.OffsetOf<MeshVertex>("Position")
                ),
                new(
                    location: 1,
                    binding: 0,
                    format: Format.R32G32Sfloat,
                    offset: (uint)Marshal.OffsetOf<MeshVertex>("TextureCoordinates")
                )
            };
        }

        protected override void LoadShaderModules()
        {
            LoadShaderModule("./Graphics/Shaders/modelVertexShader.spv", ShaderStageFlags.VertexBit);
            LoadShaderModule("./Graphics/Shaders/modelFragmentShader.spv", ShaderStageFlags.FragmentBit);
        }

        protected override PushConstantRange[] GetPushConstantRanges()
        {
            return new PushConstantRange[]
            {
                new(
                    stageFlags: ShaderStageFlags.VertexBit,
                    offset: 0,
                    size: (uint)Marshal.SizeOf<ModelPushConstant>()
                )
            };
        }

        protected override unsafe void RegisterDescriptors()
        {
            RegisterDescriptor(0, false,
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
