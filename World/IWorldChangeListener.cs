namespace Vulkano.World
{
    internal interface IWorldChangeListener
    {

        void BlockChanged(uint x, uint y, uint z);

        void LightColumnChanged(uint x, uint z, uint yFrom, uint yTo);

        void AllChanged();

    }
}
