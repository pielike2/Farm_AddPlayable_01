using Base.ServiceSystem;
using Playable;
using Playable.Configs;
using Playable.Models;
using Playable.Services;
using Playable.Signals;
using UnityEngine;
using Utility;

namespace Base
{
    public class PlayableCore : MonoSingleton<PlayableCore>
    {
        [SerializeField] private RegistryConfig _registryConfig;

        private static ServicesUpdater _servicesUpdater;

        public static ServiceLocator ServiceLocator { get; private set; }
        public RegistryConfig RegistryConfig => _registryConfig;

        protected override void Init()
        {
            base.Init();
            
            ModelsContainer.Init();
            PlayablePluginsInitializer.Init();
            
            var servicesProvider = new ServicesProvider();
            ServiceLocator = new ServiceLocator(servicesProvider.GetDefaultServices());
            ServiceLocator.InitServices();
            _servicesUpdater = new ServicesUpdater(ServiceLocator);
            
            ServiceLocator.StartDefaultServices();
        }

        private void Start()
        {
            Get.SignalBus.Publish(new SGameStarted());
        }

        private void Update()
        {
            _servicesUpdater.Tick();
        }

        private void LateUpdate()
        {
            _servicesUpdater.LateTick();
        }

        private void FixedUpdate()
        {
            _servicesUpdater.FixedTick();
        }
    }
}