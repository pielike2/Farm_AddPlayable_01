namespace Base.ServiceSystem
{
    public interface ILateTickHandler
    {
        bool IsEnabled { get; }
        void OnLateTick();
    }
}