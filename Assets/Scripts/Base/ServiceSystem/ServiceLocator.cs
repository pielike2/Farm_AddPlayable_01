using System;
using System.Collections.Generic;
using System.Linq;

namespace Base.ServiceSystem
{
    public class ServiceLocator
    {
        private readonly Dictionary<Type, IService> _servicesLookup = new Dictionary<Type, IService>();
        private readonly Dictionary<IService, ServiceInitArgs> _serviceInitArgsLookup = new Dictionary<IService, ServiceInitArgs>();

        public List<IService> ServicesByPriority { get; } = new List<IService>();

        public ServiceLocator(IEnumerable<ServiceInitArgs> serviceInitArgs)
        {
            foreach (var args in serviceInitArgs)
            {
                _servicesLookup[args.Service.GetType()] = args.Service;
                _serviceInitArgsLookup[args.Service] = args;
                ServicesByPriority.Add(args.Service);
            }
            
            ServicesByPriority = ServicesByPriority.OrderByDescending(service => _serviceInitArgsLookup[service].Priority).ToList();
        }

        public void InitServices()
        {
            foreach (var service in ServicesByPriority)
                service.Init();
        }

        public void StartDefaultServices()
        {
            foreach (var service in ServicesByPriority)
                if (_serviceInitArgsLookup[service].EnableByDefault)
                    service.Enable();
        }

        public IService GetService(Type type)
        {
            _servicesLookup.TryGetValue(type, out var result);
            return result;
        }

        public T GetService<T>() where T : class, IService
        {
            _servicesLookup.TryGetValue(typeof(T), out var result);
            return result as T;
        }

        public T GetServiceDerivedFrom<T>() where T : class
        {
            foreach (var pair in _servicesLookup)
            {
                if (typeof(T).IsAssignableFrom(pair.Key))
                    return pair.Value as T;
            }
            return null;
        }

        public IEnumerable<T> GetServicesDerivedFrom<T>() where T : class
        {
            foreach (var pair in _servicesLookup)
            {
                if (typeof(T).IsAssignableFrom(pair.Key)) 
                    yield return pair.Value as T;
            }
        }
    }
}
