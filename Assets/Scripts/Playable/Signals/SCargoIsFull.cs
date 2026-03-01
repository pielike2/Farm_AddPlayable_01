using Playable.Gameplay;

namespace Playable.Signals
{
    public struct SCargoIsFull
    {
        public BaseCargo Cargo { get; }
        
        public SCargoIsFull(BaseCargo cargo)
        {
            Cargo = cargo;
        }
    }
}