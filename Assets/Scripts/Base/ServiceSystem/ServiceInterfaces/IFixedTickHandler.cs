namespace Base.ServiceSystem
{
    public interface IFixedTickHandler
    {
        bool IsEnabled { get; }
        void OnFixedTick();
    }
}