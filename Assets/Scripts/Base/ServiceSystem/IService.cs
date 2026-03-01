namespace Base.ServiceSystem
{
    public interface IService
    {
        bool IsInitialized { get; }
        bool IsEnabled { get; }
        void Init();
        void Enable();
        void Disable();
    }

    public interface IServiceWithConfig : IService
    {
        void SetConfig(ServiceConfig config);
    }
}