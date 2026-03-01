using System;
using System.Collections;
using Base.SignalSystem;
using UnityEngine;
using Utility.Extensions;

namespace Base.ServiceSystem
{
    public abstract class Service : IService, IDisposable
    {
        private readonly CompositeDisposable _subsGeneral = new CompositeDisposable();
        private readonly CompositeDisposable _subsWhileEnabled = new CompositeDisposable();
        
        public bool IsInitialized { get; private set; }
        public bool IsEnabled { get; private set; }

        public void Init()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            
            OnInit();
        }

        public void Enable()
        {
            if (IsEnabled)
                return;
            IsEnabled = true;
            OnServiceEnable();
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;
            IsEnabled = false;
            _subsWhileEnabled.Clear();
            OnServiceDisable();
        }

        public void Dispose()
        {
            Disable();
            _subsGeneral?.Dispose();
        }

        protected virtual void OnInit()
        {
        }
        
        protected virtual void OnServiceEnable()
        {
        }
        
        protected virtual void OnServiceDisable()
        {
        }

        protected void Publish<T>(T message)
        {
            SignalBus.Default.Publish(message);
        }
        
        protected void Subscribe<T>(Action<T> action, bool duringEnabled = true)
        {
            SignalBus.Default.Subscribe(action)
                .AddTo(duringEnabled ? _subsWhileEnabled : _subsGeneral);
        }
        
        public void AddToActiveSubs(IDisposable sub)
        {
            _subsWhileEnabled.Add(sub);
        }
        
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return PlayableCore.Instance.StartCoroutine(routine);
        }
        
        public void StopCoroutine(Coroutine coroutine)
        {
            PlayableCore.Instance.StopCoroutine(coroutine);
        }
    }
}