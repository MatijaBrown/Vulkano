using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;
using Vulkano.Engine;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    internal class DebugLinePipeline : VPipeline
    {

        public DebugLinePipeline(VulkanEngine engine, Vk vk)
            : base(engine.Swapchain.RenderPass, engine, vk) { }

        protected override CullModeFlags CullMode => CullModeFlags.None;

        protected override PolygonMode PolygonMode => PolygonMode.Line;

        protected override PrimitiveTopology Topology => PrimitiveTopology.LineList;

        protected override PushConstantRange[] GetPushConstantRanges()
        {
            return new PushConstantRange[]
            {
                new PushConstantRange(
                    stageFlags: ShaderStageFlags.VertexBit,
                    offset: 0,
                    size: (uint)Marshal.SizeOf<Matrix4x4>()
                ),
            };
        }

        protected override VertexInputAttributeDescription[] GetVertexDescriptions(out VertexInputBindingDescription? bindingDescription)
        {
            bindingDescription = null;
            return Array.Empty<VertexInputAttributeDescription>();
        }

        protected override void LoadShaderModules()
        {
            LoadShaderModule("./Graphics/Shaders/debugLineVertexShader.spv", ShaderStageFlags.VertexBit);
            LoadShaderModule("./Graphics/Shaders/debugLineFragmentShader.spv", ShaderStageFlags.FragmentBit);
        }

        protected unsafe override void RegisterDescriptors()
        {
            RegisterDescriptor(0, false,
                new DescriptorSetLayoutBinding(
                    binding: 0,
                    descriptorType: DescriptorType.UniformBuffer,
                    descriptorCount: 1,
                    stageFlags: ShaderStageFlags.VertexBit,
                    pImmutableSamplers: null
                )
            );
        }

    }
}
