using Silk.NET.Maths;
using System.Numerics;
using Vulkano.Engine;
using Vulkano.Entities.Creatures;

namespace Vulkano.Utils.Maths
{
    internal class Camera
    {

        private readonly float _fieldOfView;
        private readonly float _neadPlane;
        private readonly float _farPlane;

        private readonly Display _display;

        public Vector3 Facing { get; set; } = new Vector3(0.0f, 0.0f, -1.0f);

        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);

        public Matrix4x4 ViewMatrix { get; private set; }

        public Matrix4x4 ProjectionMatrix { get; private set; }

        public Matrix4x4 ViewProjection { get; private set; }

        public IntersectionTester? Intersection { get; set; }

        public Camera(float fieldOfView, float nearPlane, float farPlane, Display display)
        {
            _fieldOfView = fieldOfView;
            _neadPlane = nearPlane;
            _farPlane = farPlane;
            _display = display;
        }

        public void Update()
        {
            ViewMatrix = Matrix4x4.CreateLookAt(Position, Position + Vector3.Normalize(Facing), Vector3.UnitY);

            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, _display.AspectRatio, _neadPlane, _farPlane);
            projection.M22 *= -1;
            ProjectionMatrix = projection;

            ViewProjection = ViewMatrix * ProjectionMatrix;
        }

        public void MoveToPlayer(Player player)
        {
            Position = player.Eyes;
            Facing = player.Facing;
        }

        public bool Pick(float stepSize, float maxDistance, out Vector3? resultPosition)
        {
            resultPosition = null;
            if (Intersection == null)
            {
                return false;
            }

            Vector3 step = Vector3.Normalize(Facing) * stepSize;
            Vector3 currentPosition = Position + step;
            while ((currentPosition - Position).Length() <= maxDistance)
            {
                if (Intersection(Position, currentPosition))
                {
                    resultPosition = currentPosition;
                    return true;
                }
                currentPosition += step;
            }
            return false;
        }

    }
}
