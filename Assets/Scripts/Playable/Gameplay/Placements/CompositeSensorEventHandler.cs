using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Placements
{
    public class CompositeSensorEventHandler : MonoBehaviour
    {
        [SerializeField] private HashId[] _targetSenseIds = new []{new HashId("MainCharacter")};
        [SerializeField] private List<BaseSensor> _sensorsStart = new List<BaseSensor>();
        [SerializeField] private List<BaseSensor> _sensorsComplete = new List<BaseSensor>();
        [SerializeField] private bool _onlyPlayer = true;

        [SerializeField] private UnityEvent _onStartInteraction;
        [SerializeField] private UnityEvent _onStopInteraction;

        private readonly HashSet<ISensorTarget> _activeStartPlacements = new HashSet<ISensorTarget>();
        private readonly HashSet<ISensorTarget> _activeCompletePlacements = new HashSet<ISensorTarget>();
        
        private bool _isCurrentlyOpen = false;
        private bool _isForcedOpen = false;

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            for (int i = 0; i < _targetSenseIds.Length; i++)
            {
                foreach (var sensor in _sensorsStart)
                {
                    sensor.GetFilter(_targetSenseIds[i]).OnAddTarget += OnStartInteraction;
                    sensor.GetFilter(_targetSenseIds[i]).OnRemoveTarget += OnStartInteractionRemove;
                }
                
                foreach (var sensor in _sensorsComplete)
                {
                    sensor.GetFilter(_targetSenseIds[i]).OnAddTarget += OnStopInteraction;
                    sensor.GetFilter(_targetSenseIds[i]).OnRemoveTarget += OnStopInteractionRemove;
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            foreach (var sensor in _sensorsStart)
            {
                if (sensor != null)
                {
                    sensor.OnAddTarget -= OnStartInteraction;
                    sensor.OnRemoveTarget -= OnStartInteractionRemove;
                }
            }

            foreach (var sensor in _sensorsComplete)
            {
                if (sensor != null)
                {
                    sensor.OnAddTarget -= OnStopInteraction;
                    sensor.OnRemoveTarget -= OnStopInteractionRemove;
                }
            }
        }

        public void ForceStart()
        {
            _isForcedOpen = true;
            CheckAndUpdateState();
        }

        public void ForceCansel()
        {
            _isForcedOpen = false;
            CheckAndUpdateState();
        }

        private void OnStartInteraction(ISensorTarget target)
        {
            _activeStartPlacements.Add(target);
            CheckAndUpdateState();
        }

        private void OnStartInteractionRemove(ISensorTarget target)
        {
            _activeStartPlacements.Remove(target);
            CheckAndUpdateState();
        }

        private void OnStopInteraction(ISensorTarget target)
        {
            _activeCompletePlacements.Add(target);
            CheckAndUpdateState();
        }

        private void OnStopInteractionRemove(ISensorTarget target)
        {
            _activeCompletePlacements.Remove(target);
            CheckAndUpdateState();
        }

        private void CheckAndUpdateState()
        {
            bool shouldOpen = _isForcedOpen || _activeStartPlacements.Count > 0;
            bool shouldClose = _activeCompletePlacements.Count > 0;

            // Приоритет открытия над закрытием
            bool newState = shouldOpen || (!shouldClose && _isCurrentlyOpen);

            if (newState != _isCurrentlyOpen)
            {
                _isCurrentlyOpen = newState;
                
                if (_isCurrentlyOpen)
                {
                    _onStartInteraction?.Invoke();
                }
                else
                {
                    _onStopInteraction?.Invoke();
                }
            }
        }
    }
}