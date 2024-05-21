using System.Numerics;

namespace Vulkano.Utils.Physics
{
    internal readonly struct CollisionResolution
    {

        public readonly Vector3 Axis;
        public readonly float Scale;
        public readonly float NewAxisPosition;

        public Vector3 Mask => Vector3.One - Axis;

        public CollisionResolution(Vector3 axis, float scale)
        {
            Axis = axis;
            Scale = scale;
        }

    }
}
