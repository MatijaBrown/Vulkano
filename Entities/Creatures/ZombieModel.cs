using Silk.NET.Vulkan;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Graphics.ModelRenderSystem;
using Vulkano.Utils.Maths;

namespace Vulkano.Entities.Creatures
{
    internal class ZombieModel : IDisposable
    {

        public const float WIDTH = 0.6f;
        public const float HEIGHT = 1.8f;

        private const float SCALE = 0.058333334f;

        private readonly ModelRenderer _renderer;

        private readonly VImage _textureMap;
        private readonly DescriptorSet _textureMapDescriptor;

        private readonly Model _head;
        private readonly Model _body;
        private readonly Model _leftArm, _rightArm;
        private readonly Model _leftLeg, _rightLeg;

        public ZombieModel(ModelRenderer renderer, VulkanEngine engine, Vk vk)
        {
            _renderer = renderer;

            _textureMap = new VImage("./Resources/Textures/Zombie.png", engine, vk);
            _textureMap.CreateSampler(Filter.Nearest);
            _textureMapDescriptor = engine.AllocateDescriptor(renderer.Pipeline.GetSetLayout(0));
            engine.WriteDescriptorSet(_textureMapDescriptor, 0, _textureMap.ImageInfo());

            _head = new Model(CubeMesh(-4.0f, -8.0f, -4.0f, 0.0f, 0.0f, 8.0f, 8.0f, 8.0f, engine, vk), _textureMapDescriptor);
            _body = new Model(CubeMesh(-4.0f, 0.0f, -2.0f, 16.0f, 16.0f, 8.0f, 12.0f, 4.0f, engine, vk), _textureMapDescriptor);
            _leftArm = new Model(CubeMesh(-3.0f, -2.0f, -2.0f, 40.0f, 16.0f, 4.0f, 12.0f, 4.0f, engine, vk), _textureMapDescriptor);
            _rightArm = new Model(CubeMesh(-1.0f, -2.0f, -2.0f, 40.0f, 16.0f, 4.0f, 12.0f, 4.0f, engine, vk), _textureMapDescriptor);
            _leftLeg = new Model(CubeMesh(-2.0f, 0.0f, -2.0f, 0.0f, 16.0f, 4.0f, 12.0f, 4.0f, engine, vk), _textureMapDescriptor);
            _rightLeg = new Model(CubeMesh(-2.0f, 0.0f, -2.0f, 0.0f, 16.0f, 4.0f, 12.0f, 4.0f, engine, vk), _textureMapDescriptor);
        }

        public void RenderAt(Transform transform, float time)
        {
            transform = transform.PrependTranslation(new Vector3(0.0f, HEIGHT / 32.0f * 24.0f, 0.0f)).PrependRotationX(MathF.PI).PrependScale(SCALE);
            
            _renderer.RenderModel(_head, transform
                .PrependRotationX(MathF.Sin(time) * 0.8f)
                .PrependRotationY(MathF.Sin(time * 0.83f)));
            _renderer.RenderModel(_body, transform);
            _renderer.RenderModel(_leftArm, transform
                .PrependTranslation(new Vector3(-5.0f, 2.0f, 0.0f))
                .PrependRotationX(MathF.Sin(time * 2.0f / 3.0f + MathF.PI) * 2.0f)
                .PrependRotationZ(MathF.Sin(time * 0.2312f) + 1.0f));
            _renderer.RenderModel(_rightArm, transform
                .PrependTranslation(new Vector3(5.0f, 2.0f, 0.0f))
                .PrependRotationX(MathF.Sin(time * 2.0f / 3.0f) * 2.0f)
                .PrependRotationZ(MathF.Sin(time * 0.2312f) - 1.0f));
            _renderer.RenderModel(_leftLeg, transform
                .PrependTranslation(new Vector3(-2.0f, 12.0f, 0.0f))
                .PrependRotationX(MathF.Sin(time * 2.0f / 3.0f) * 1.4f));
            _renderer.RenderModel(_rightLeg, transform
                .PrependTranslation(new Vector3(2.0f, 12.0f, 0.0f))
                .PrependRotationX(MathF.Sin(time * 2.0f / 3.0f + MathF.PI) * 1.4f));
        }

        public void Dispose()
        {
            _head.Dispose();
            _body.Dispose();
            _leftArm.Dispose();
            _rightArm.Dispose();
            _leftLeg.Dispose();
            _rightLeg.Dispose();
            _textureMap.Dispose();
        }

        private static Mesh CubeMesh(float x, float y, float z, float u, float v, float w, float h, float d, VulkanEngine engine, Vk vk)
        {
            var vertices = new MeshVertex[]
            {
                // Top
                new(x + w, y, z + d, u + d, v, 64.0f, 32.0f),
                new(x + w, y, z, u + d, v + d, 64.0f, 32.0f),
                new(x, y, z, u + d + w, v + d, 64.0f, 32.0f),
                new(x, y, z + d, u + d + w, v, 64.0f, 32.0f),

                // Bottom
                new(x + w, y + h, z, u + d + w, v + d, 64.0f, 32.0f),
                new(x + w, y + h, z + d, u + d + w, v, 64.0f, 32.0f),
                new(x, y + h, z + d, u + d + 2 * w, v, 64.0f, 32.0f),
                new(x, y + h, z, u + d + 2 * w, v + d, 64.0f, 32.0f),

                // Left
                new(x + w, y, z + d, u, v + d, 64.0f, 32.0f),
                new(x + w, y + h, z + d, u, v + d + h, 64.0f, 32.0f),
                new(x + w, y + h, z, u + d, v + d + h, 64.0f, 32.0f),
                new(x + w, y,  z, u + d, v + d, 64.0f, 32.0f),

                // Right
                new(x, y + h, z, u + d + w, v + d + h, 64.0f, 32.0f),
                new(x, y + h, z + d, u + 2 * d + w, v + d + h, 64.0f, 32.0f),
                new(x, y, z + d, u + 2 * d + w, v + d, 64.0f, 32.0f),
                new(x, y, z, u + d + w, v + d, 64.0f, 32.0f),

                // Back
                new(x, y, z + d, u + 2 * d + w, v + d, 64.0f, 32.0f),
                new(x, y + h, z + d, u + 2 * d + w, v + d + h, 64.0f, 32.0f),
                new(x + w, y + h, z + d, u + 2 * d + 2 * w, v + d + h, 64.0f, 32.0f),
                new(x + w, y, z + d, u + 2 * d + 2 * w, v + d, 64.0f, 32.0f),

                // Front
                new(x + w, y, z, u + d, v + d, 64.0f, 32.0f),
                new(x + w, y + h, z, u + d, v + d + h, 64.0f, 32.0f),
                new(x, y + h, z, u + d + w, v + d + h, 64.0f, 32.0f),
                new(x, y, z, u + d + w, v + d, 64.0f, 32.0f)
            };

            var indices = new ushort[]
            {
                // Top
                2, 1, 0, 3, 2, 0,

                // Bottom
                6, 5, 4, 7, 6, 4,

                // Left
                10, 9, 8, 11, 10, 8,

                // Right
                14, 13, 12, 15, 14, 12,

                // Back
                18, 17, 16, 19, 18, 16,

                // Front
                22, 21, 20, 23, 22, 20
            };

            return new Mesh(engine, vk, vertices, indices);
        }

    }
}
