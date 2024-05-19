using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Vulkano.Engine;

namespace Vulkano.Graphics.SelectionRenderSystem
{
    internal class SelectionPipeline : VPipeline
    {

        public SelectionPipeline(VulkanEngine engine, Vk vk)
            : base(engine.Swapchain.RenderPass, engine, vk) { }

        protected override PipelineColorBlendAttachmentState ColourBlendAttachment => new PipelineColorBlendAttachmentState(
            blendEnable: true,
            srcColorBlendFactor: BlendFactor.SrcAlpha,
            dstColorBlendFactor: BlendFactor.OneMinusSrcAlpha,
            colorBlendOp: BlendOp.Add,
            srcAlphaBlendFactor: BlendFactor.One,
            dstAlphaBlendFactor: BlendFactor.Zero,
            alphaBlendOp: BlendOp.Add,
            colorWriteMask: ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
        );

        protected override PushConstantRange[] GetPushConstantRanges()
        {
            return new PushConstantRange[]
            {
                new PushConstantRange(
                    stageFlags: ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                    offset: 0,
                    size: (uint)Marshal.SizeOf<SelectionPushConstant>()
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
            LoadShaderModule("./Graphics/Shaders/selectionVertexShader.spv", ShaderStageFlags.VertexBit);
            LoadShaderModule("./Graphics/Shaders/selectionFragmentShader.spv", ShaderStageFlags.FragmentBit);
        }

        protected override void RegisterDescriptors() { }

    }
}
