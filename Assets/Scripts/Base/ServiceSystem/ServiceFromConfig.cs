using UnityEngine;

namespace Base.ServiceSystem
{
    public class ServiceConfig : ScriptableObject
    {
        [SerializeField] public bool enableByDefault = true;
        [SerializeField] public int priority = 0;

        public IService InitService()
        {
            var service = CreateService();
            service.SetConfig(this);
            return service;
        }

        protected virtual IServiceWithConfig CreateService()
        {
            return null;
        }
    }
    
    public abstract class ServiceFromConfig<T> : Service, IServiceWithConfig
        where T : ServiceConfig
    {
        public T Config { get; private set; }

        public void SetConfig(ServiceConfig config)
        {
            Config = config as T;
        }
    }
}