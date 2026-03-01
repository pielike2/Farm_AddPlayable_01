using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class BasicUnityEventsCaller : MonoBehaviour
    {
        [Serializable]
        public class DelayedEvent
        {
            public float delay;
            public UnityEvent @event;
        }
        
        [SerializeField] private UnityEvent _onAwake;
        [SerializeField] private UnityEvent _onStart;
        [SerializeField] private UnityEvent _onEnable;
        [SerializeField] private UnityEvent _onDisable;
        [SerializeField] private DelayedEvent[] _delayedEventsOnStart = new DelayedEvent[0];
        [SerializeField] private DelayedEvent[] _delayedEventsOnEnable = new DelayedEvent[0];
        
        private void Awake()
        {
            _onAwake.Invoke();            
        }

        private void Start()
        {
            _onStart.Invoke();

            foreach (var item in _delayedEventsOnStart)
                StartCoroutine(DelayedEventRoutine(item));
        }

        private void OnEnable()
        {
            _onEnable.Invoke();

            foreach (var item in _delayedEventsOnEnable)
                StartCoroutine(DelayedEventRoutine(item));
        }

        private void OnDisable()
        {
            _onDisable.Invoke();
        }

        private IEnumerator DelayedEventRoutine(DelayedEvent e)
        {
            yield return new WaitForSeconds(e.delay);
            e.@event.Invoke();
        }
    }
}