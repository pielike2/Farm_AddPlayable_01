namespace Utility
{
    public interface ITickByDemand
    {
        void Tick();
        void EnterTick();
        void ExitTick();
    }
}