using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Vulkano.Engine.Exceptions;

namespace Vulkano.Engine
{
    internal abstract class VPipeline : IDisposable
    {

        private readonly IDictionary<uint, DescriptorSetLayout> _setLayouts = new Dictionary<uint, DescriptorSetLayout>();

        private readonly IList<ShaderModule> _shaderModules = new List<ShaderModule>();
        private readonly IList<PipelineShaderStageCreateInfo> _shaderStages = new List<PipelineShaderStageCreateInfo>();

        private readonly VulkanEngine _engine;
        private readonly Vk _vk;

        private readonly RenderPass _renderPass;

        private readonly Pipeline _pipeline;

        private readonly PipelineLayout _pipelineLayout;

        protected VPipeline(RenderPass renderPass, VulkanEngine engine, Vk vk)
        {
            _renderPass = renderPass;
            _engine = engine;
            _vk = vk;

            LoadShaderModules();

            RegisterDescriptors();
            _pipelineLayout = CreatePipelineLayout();

            _pipeline = CreatePipeline();
        }

        protected virtual PipelineColorBlendAttachmentState ColourBlendAttachment => new (
            colorWriteMask: ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            blendEnable: false,
            srcColorBlendFactor: BlendFactor.One,
            dstColorBlendFactor: BlendFactor.Zero,
            colorBlendOp: BlendOp.Add,
            srcAlphaBlendFactor: BlendFactor.One,
            dstAlphaBlendFactor: BlendFactor.Zero,
            alphaBlendOp: BlendOp.Add
        );

        protected virtual PrimitiveTopology Topology => PrimitiveTopology.TriangleList;

        protected virtual PolygonMode PolygonMode => PolygonMode.Fill;

        protected virtual CullModeFlags CullMode => CullModeFlags.BackBit;

        protected virtual FrontFace FrontFace => FrontFace.CounterClockwise;

        protected virtual bool DepthTest => true;

        protected virtual bool StencilTest => false;

        protected virtual PipelineTessellationStateCreateInfo? TessellationState => null;

        protected abstract void LoadShaderModules();

        protected abstract VertexInputAttributeDescription[] GetVertexDescriptions(out VertexInputBindingDescription? bindingDescription);

        protected unsafe abstract void RegisterDescriptors();

        protected virtual PushConstantRange[] GetPushConstantRanges()
        {
            return Array.Empty<PushConstantRange>();
        }

        protected unsafe void LoadShaderModule(string shaderFile, ShaderStageFlags stage)
        {
            byte[] shaderCode = File.ReadAllBytes(shaderFile);

            var createInfo = new ShaderModuleCreateInfo(
                codeSize: (uint)shaderCode.Length
            );
            fixed (byte* ptr = shaderCode)
            {
                createInfo.PCode = (uint*)ptr;
            }

            if (_vk.CreateShaderModule(_engine.Device, createInfo, null, out ShaderModule shaderModule) != Result.Success)
            {
                throw new VulkanException("Failed to create ShaderModule \"" + shaderFile + "\"!");
            }
            _shaderModules.Add(shaderModule);

            var shaderStage = new PipelineShaderStageCreateInfo(
                stage: stage,
                module: shaderModule,
                pName: (byte*)SilkMarshal.StringToPtr("main")
            );
            _shaderStages.Add(shaderStage);
        }

        private DescriptorSetLayout[] GetSortedSetLayouts()
        {
            uint[] desriptorSetLayoutIndices = _setLayouts.Keys.ToArray();
            Array.Sort(desriptorSetLayoutIndices);
            DescriptorSetLayout[] descriptorSetLayouts = new DescriptorSetLayout[desriptorSetLayoutIndices.Length];
            for (int i = 0; i < desriptorSetLayoutIndices.Length; i++)
            {
                descriptorSetLayouts[i] = _setLayouts[desriptorSetLayoutIndices[i]];
            }
            return descriptorSetLayouts;
        }

        private unsafe PipelineLayout CreatePipelineLayout()
        {
            PushConstantRange[] pushConstantRanges = GetPushConstantRanges();

            var createInfo = new PipelineLayoutCreateInfo(sType: StructureType.PipelineLayoutCreateInfo);
            fixed (PushConstantRange* ptr = pushConstantRanges)
            {
                createInfo.PushConstantRangeCount = (uint)pushConstantRanges.Length;
                createInfo.PPushConstantRanges = ptr;
            }

            DescriptorSetLayout[] descriptorSetLayouts = GetSortedSetLayouts();
            fixed (DescriptorSetLayout* ptr = descriptorSetLayouts)
            {
                createInfo.SetLayoutCount = (uint)descriptorSetLayouts.Length;
                createInfo.PSetLayouts = ptr;
            }

            if (_vk.CreatePipelineLayout(_engine.Device, createInfo, null, out PipelineLayout pipelineLayout) != Result.Success)
            {
                throw new VulkanException("Failed to create PipelineLayout!");
            }

            return pipelineLayout;
        }

        protected unsafe void RegisterDescriptor(uint set, bool pushDescriptor = false, params DescriptorSetLayoutBinding[] bindings)
        {
            if (_setLayouts.ContainsKey(set))
            {
                throw new InvalidOperationException("DescriptorSetLayout 'set=" + set + "' already specified for this pipeline!");
            }
            var createInfo = new DescriptorSetLayoutCreateInfo(
                flags: pushDescriptor ? DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr : 0,
                bindingCount: (uint)bindings.Length
            );
            fixed (DescriptorSetLayoutBinding* ptr = bindings)
            {
                createInfo.PBindings = ptr;
            }
            if (_vk.CreateDescriptorSetLayout(_engine.Device, createInfo, null, out DescriptorSetLayout descriptorSetLayout) != Result.Success)
            {
                throw new VulkanException("Failed to create DescriptorSetLayout!");
            }
            _setLayouts.Add(set, descriptorSetLayout);
        }

        private unsafe Pipeline CreatePipeline()
        {
            DynamicState* dynamicStates = stackalloc DynamicState[2]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };
            var dynamicState = new PipelineDynamicStateCreateInfo(
                dynamicStateCount: 2,
                pDynamicStates: dynamicStates
            );

            var vertexInputInfo = new PipelineVertexInputStateCreateInfo(vertexAttributeDescriptionCount: 0);
            VertexInputAttributeDescription[] attributeDescriptions = GetVertexDescriptions(out VertexInputBindingDescription? bindingDescription);
            VertexInputBindingDescription bDescription;
            if (bindingDescription.HasValue)
            {
                bDescription = bindingDescription.Value;
                vertexInputInfo.VertexAttributeDescriptionCount = 1;
                vertexInputInfo.PVertexBindingDescriptions = &bDescription;
                fixed (VertexInputAttributeDescription* ptr = attributeDescriptions)
                {
                    vertexInputInfo.VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length;
                    vertexInputInfo.PVertexAttributeDescriptions = ptr;
                }
            }

            var inputAssembly = new PipelineInputAssemblyStateCreateInfo(
                topology: Topology,
                primitiveRestartEnable: false
            );

            var viewportState = new PipelineViewportStateCreateInfo(
                viewportCount: 1,
                scissorCount: 1
            );

            var rasteriser = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: false,

                rasterizerDiscardEnable: false,

                polygonMode: PolygonMode,

                lineWidth: 1.0f,

                cullMode: CullMode,
                frontFace: FrontFace,

                depthBiasEnable: false,
                depthBiasConstantFactor: 0.0f,
                depthBiasClamp: 0.0f,
                depthBiasSlopeFactor: 0.0f
            );

            var multisampling = new PipelineMultisampleStateCreateInfo(
                sampleShadingEnable: false,
                rasterizationSamples: SampleCountFlags.Count1Bit,
                minSampleShading: 1.0f,
                pSampleMask: null,
                alphaToCoverageEnable: false,
                alphaToOneEnable: false
            );

            PipelineColorBlendAttachmentState colourBlendAttachment = ColourBlendAttachment;

            var colourBlending = new PipelineColorBlendStateCreateInfo(
                logicOpEnable: false,
                logicOp: LogicOp.Copy,
                attachmentCount: 1,
                pAttachments: &colourBlendAttachment
            );

            var depthStencil = new PipelineDepthStencilStateCreateInfo(
                depthTestEnable: DepthTest,
                depthWriteEnable: DepthTest,

                depthCompareOp: CompareOp.Less,

                depthBoundsTestEnable: false,
                minDepthBounds: 0.0f,
                maxDepthBounds: 1.0f,

                stencilTestEnable: StencilTest,
                front: null,
                back: null
            );

            PipelineTessellationStateCreateInfo? tesselationState = TessellationState;
            PipelineTessellationStateCreateInfo tessPtrSrc = tesselationState ?? default;

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pVertexInputState: &vertexInputInfo,
                pInputAssemblyState: &inputAssembly,
                pViewportState: &viewportState,
                pRasterizationState: &rasteriser,
                pMultisampleState: &multisampling,
                pDepthStencilState: (DepthTest || StencilTest) ? &depthStencil : null,
                pColorBlendState: &colourBlending,
                pDynamicState: &dynamicState,
                pTessellationState: tesselationState.HasValue ? &tessPtrSrc : null,

                layout: _pipelineLayout,

                renderPass: _renderPass,
                subpass: 0,

                basePipelineHandle: null,
                basePipelineIndex: -1
            );
            PipelineShaderStageCreateInfo[] shaderStages = _shaderStages.ToArray();
            fixed (PipelineShaderStageCreateInfo* ptr = shaderStages)
            {
                pipelineCreateInfo.StageCount = (uint)shaderStages.Length;
                pipelineCreateInfo.PStages = ptr;
            }

            if (_vk.CreateGraphicsPipelines(_engine.Device, new PipelineCache(handle: null), 1, pipelineCreateInfo, null, out Pipeline pipeline) != Result.Success)
            {
                throw new VulkanException("Failed t ocreate graphics Pipeline!");
            }

            return pipeline;
        }

        public DescriptorSetLayout GetSetLayout(uint setIndex)
        {
            return _setLayouts[setIndex];
        }

        public void Bind(CommandBuffer cmd)
        {
            _vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline);
        }

        public unsafe void PushConstants<T>(ShaderStageFlags shader, T constants, CommandBuffer cmd)
            where T : unmanaged
        {
            _vk.CmdPushConstants(cmd, _pipelineLayout, shader, 0, (uint)Marshal.SizeOf<T>(), &constants);
        }

        public unsafe void BindDescriptorSet(uint set, DescriptorSet descriptor, CommandBuffer cmd)
        {
            _vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics, _pipelineLayout, set, 1, descriptor, 0, null);
        }

        public unsafe void UpdateDescriptorSet<T>(uint set, uint binding, VUniformValue<T> value, CommandBuffer cmd)
            where T : unmanaged
        {
            DescriptorBufferInfo bufferInfo = value.BufferInfo();

            var descriptorWrite = new WriteDescriptorSet(
                dstSet: null,
                dstBinding: binding,
                dstArrayElement: 0,

                descriptorType: DescriptorType.UniformBuffer,
                descriptorCount: 1,

                pBufferInfo: &bufferInfo,
                pImageInfo: null,
                pTexelBufferView: null
            );

            _engine.ExtPushDescriptor.CmdPushDescriptorSet(cmd, PipelineBindPoint.Graphics, _pipelineLayout, set, 1, descriptorWrite);
        }

        public unsafe void Dispose()
        {
            _vk.DestroyPipeline(_engine.Device, _pipeline, null);

            foreach (DescriptorSetLayout descriptorLayout in _setLayouts.Values)
            {
                _vk.DestroyDescriptorSetLayout(_engine.Device, descriptorLayout, null);
            }
            _vk.DestroyPipelineLayout(_engine.Device, _pipelineLayout, null);

            foreach (ShaderModule shaderModule in _shaderModules)
            {
                _vk.DestroyShaderModule(_engine.Device, shaderModule, null);
            }
        }

    }
}
