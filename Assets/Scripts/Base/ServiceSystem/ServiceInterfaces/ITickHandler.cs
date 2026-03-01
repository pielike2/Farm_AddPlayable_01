namespace Base.ServiceSystem
{
    public interface ITickHandler
    {
        bool IsEnabled { get; }
        void OnTick();
    }
}