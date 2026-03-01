using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utility.Extensions;

namespace Base.SignalSystem
{
    public class ScriptableSignalListener : MonoBehaviour
    {
        [Serializable]
        public class ListenerReaction
        {
            public ScriptableSignal signal;
            public UnityEvent action;
        }

        [SerializeField] private List<ListenerReaction> _reactions = new List<ListenerReaction>();

        private readonly CompositeDisposable _subs = new CompositeDisposable();
        
        private void OnEnable()
        {
            foreach (var item in _reactions)
            {
                Get.SignalBus.Subscribe<SScriptableSignal>(s =>
                {
                    if (s.Signal == item.signal)
                        item.action.Invoke();
                }).AddTo(_subs);
            }
        }

        private void OnDisable()
        {
            _subs.Clear();
        }
    }
}