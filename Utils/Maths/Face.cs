using System.Numerics;

namespace Vulkano.Utils.Maths
{

    internal readonly struct Face
    {

        public static readonly Face Top = new(0, Vector3.UnitY);
        public static readonly Face Bottom = new(1, -Vector3.UnitY);
        public static readonly Face West = new(2, -Vector3.UnitX);
        public static readonly Face East = new(3, Vector3.UnitX);
        public static readonly Face South = new(4, -Vector3.UnitZ);
        public static readonly Face North = new(5, Vector3.UnitZ);

        public readonly uint Index;
        public readonly Vector3 Normal;

        private Face(uint index, Vector3 normal)
        {
            Index = index;
            Normal = normal;
        }

        public static explicit operator Face(uint index)
        {
            return index switch
            {
                0 => Top,
                1 => Bottom,
                2 => West,
                3 => East,
                4 => South,
                5 => North,
                _ => throw new ArgumentException()
            };
        }

        public static implicit operator uint(Face face)
        {
            return face.Index;
        }

    }
}
