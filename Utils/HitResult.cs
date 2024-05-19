using Vulkano.Utils.Maths;

namespace Vulkano.Utils
{
    internal struct HitResult
    {

        public uint X;
        public uint Y;
        public uint Z;

        public uint PreviousX;
        public uint PreviousY;
        public uint PreviousZ;

        public HitResult(uint x, uint y, uint z, uint previousX, uint previousY, uint previousZ)
        {
            X = x;
            Y = y;
            Z = z;
            PreviousX = previousX;
            PreviousY = previousY;
            PreviousZ = previousZ;
        }

        public Face GetFace()
        {
            if (PreviousY == Y + 1)
            {
                return Face.Top;
            }
            else if (PreviousY == Y - 1)
            {
                return Face.Bottom;
            }
            else if (PreviousX == X - 1)
            {
                return Face.West;
            }
            else if (PreviousX == X + 1)
            {
                return Face.East;
            }
            else if (PreviousZ == Z - 1)
            {
                return Face.South;
            }
            else if (PreviousZ == Z + 1)
            {
                return Face.North;
            }
            throw new ApplicationException("Should not reach here!");
        }

    }
}
