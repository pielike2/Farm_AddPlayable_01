namespace Base.ServiceSystem
{
    public readonly struct ServiceInitArgs
    {
        public IService Service { get; }
        public int Priority { get; }
        public bool EnableByDefault { get; }
        
        public ServiceInitArgs(IService service, int priority = 0, bool enableByDefault = true)
        {
            Service = service;
            Priority = priority;
            EnableByDefault = enableByDefault;
        }
    }
}