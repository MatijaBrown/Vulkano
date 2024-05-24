using System.Numerics;

namespace Vulkano.Utils.Maths
{
    internal readonly struct Transform
    {

        public static readonly Transform Identity = new(Matrix4x4.Identity);

        public readonly Matrix4x4 TransformationMatrix = Matrix4x4.Identity;

        public Transform(Matrix4x4 transformationMatrix)
        {
            TransformationMatrix = transformationMatrix;
        }

        public Transform PrependTranslation(Vector3 translation)
        {
            return new Transform(Matrix4x4.CreateTranslation(translation) * TransformationMatrix);
        }

        public Transform PrependRotation(Vector3 axisRotation)
        {
            return new Transform(Matrix4x4.CreateFromYawPitchRoll(axisRotation.Y, axisRotation.X, axisRotation.Z) * TransformationMatrix);
        }

        public Transform PrependRotation(Quaternion rotation)
        {
            return new Transform(Matrix4x4.CreateFromQuaternion(rotation) * TransformationMatrix);
        }

        public Transform PrependRotationX(float rotationX)
        {
            return new Transform(Matrix4x4.CreateRotationX(rotationX) * TransformationMatrix);
        }

        public Transform PrependRotationY(float rotationY)
        {
            return new Transform(Matrix4x4.CreateRotationY(rotationY) * TransformationMatrix);
        }

        public Transform PrependRotationZ(float rotationZ)
        {
            return new Transform(Matrix4x4.CreateRotationZ(rotationZ) * TransformationMatrix);
        }

        public Transform PrependScale(float scale)
        {
            return new Transform(Matrix4x4.CreateScale(scale) * TransformationMatrix);
        }

        public Transform PrependTransform(Matrix4x4 matrix)
        {
            return new Transform(matrix * TransformationMatrix);
        }

        public Transform PrependTransform(Transform transform)
        {
            return new Transform(transform.TransformationMatrix * TransformationMatrix);
        }

        public Transform AppendTransform(Matrix4x4 matrix)
        {
            return new Transform(TransformationMatrix * matrix);
        }

        public Transform AppendTransform(Transform transform)
        {
            return new Transform(TransformationMatrix * transform.TransformationMatrix);
        }

    }
}
