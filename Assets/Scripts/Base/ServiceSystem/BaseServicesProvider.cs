using System.Collections.Generic;
using Playable;

namespace Base.ServiceSystem
{
    public abstract class BaseServicesProvider
    {
        private HashSet<ServiceInitArgs> _services;
        
        public IEnumerable<ServiceInitArgs> GetDefaultServices()
        {
            _services = new HashSet<ServiceInitArgs>();
            
            CreatePlainServices();
            AddScriptableObjectServices();

            return _services;
        }

        private void AddScriptableObjectServices()
        {
            foreach (var config in Get.RegistryConfig.services.services)
            {
                if (config == null) continue;
                _services.Add(new ServiceInitArgs(config.InitService(), config.priority, config.enableByDefault));
            }
        }

        protected abstract void CreatePlainServices();

        protected void CreatePlainService<T>() where T : Service, new()
        {
            CreatePlainService<T>(0, true);
        }
        
        protected void CreatePlainService<T>(int priority) where T : Service, new()
        {
            CreatePlainService<T>(priority, true);
        }

        protected void CreatePlainService<T>(int priority, bool enableByDefault) where T : Service, new()
        {
            var service = new T();
            _services.Add(new ServiceInitArgs(service, priority, enableByDefault));
        }
    }
}