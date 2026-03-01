namespace Playable.Gameplay.Production
{
    public interface IProductionMachine
    {
        bool IsMachineActive { get; }
        void Toggle(bool active);
    }
}