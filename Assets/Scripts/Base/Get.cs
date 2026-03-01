using System;
using System.Collections.Generic;
using Base.ServiceSystem;
using Base.SignalSystem;
using Playable;
using Playable.Configs;
using Playable.Models;
using UnityEngine;

namespace Base
{
    public static class Get
    {
        private static readonly RaycastHit[] _hitsBuffer = new RaycastHit[30];
        private static readonly Collider[] _collidersBuffer = new Collider[30];

        public static RaycastHit[] HitsBuffer => _hitsBuffer;
        public static Collider[] CollidersBuffer => _collidersBuffer;
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

        public static SignalBus SignalBus => SignalBus.Default;
        
        public static readonly HashSet<string> UsedAnalyticsEvents = new HashSet<string>();

        #region CONFIGS

        public static RegistryConfig RegistryConfig => PlayableCore.Instance.RegistryConfig;

        public static CommonConfig Common => RegistryConfig.common;
        public static BalanceConfig Balance => RegistryConfig.balance;
        public static VisualsConfig Visuals => RegistryConfig.visuals;

        #endregion


        #region MODELS

        public static InputModel Input => ModelsContainer.Input;
        public static SceneBasicVariables SceneVars => ModelsContainer.SceneVariables;

        #endregion


        #region SERVICES

        public static T Service<T>() where T : class, IService
        {
            return PlayableCore.ServiceLocator.GetService<T>();
        }

        public static IService Service(Type type)
        {
            return PlayableCore.ServiceLocator.GetService(type);
        }

        #endregion
    }
}