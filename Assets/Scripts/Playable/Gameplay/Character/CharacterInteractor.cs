using System.Collections.Generic;
using System.Linq;
using Playable.Gameplay.Placements;
using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Character
{
    public interface IInteractor : IUnityObject
    {
    }

    public class CharacterInteractor : MonoBehaviour, IInteractor
    {
        [SerializeField] private CharacterMovement _characterMovement;
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] private HashId _targetSenseId;

        private SensorFilter _sensorFilter;
        private bool _hasCurrentPlacement;
        private Placement _currentPlacement;
        private bool _isUsingPlacement;
        private HashSet<Placement> _placements = new HashSet<Placement>();

        private void Awake()
        {
            _sensorFilter = _sensor.GetFilter(_targetSenseId);
            _sensorFilter.OnAddTarget += OnAddTarget;
            _sensorFilter.OnRemoveTarget += OnRemoveTarget;
        }

        private void Update()
        {
            UpdateCurrentPlacement();
        }

        private void UpdateCurrentPlacement()
        {
            if (!_hasCurrentPlacement)
                return;
            
            if (_isUsingPlacement)
            {
                if (_currentPlacement.NeedStopMovement 
                    && _characterMovement.IsMoving 
                    && _characterMovement.AutoMovementState == CharacterAutoMovementState.NonAuto)
                {
                    StopInteraction();
                }
            }
            else
            {
                if (!_currentPlacement.NeedStopMovement ||
                    _currentPlacement.NeedStopMovement && !_characterMovement.IsMoving)
                {
                    StartInteraction();
                }
            }
        }

        private void OnAddTarget(ISensorTarget target)
        {
            var placement = target as Placement;
            if (placement)
            {
                _placements.Add(placement);
                if (_isUsingPlacement && _currentPlacement != placement)
                    StopInteraction();
                _currentPlacement = placement;
                _hasCurrentPlacement = true;
                UpdateCurrentPlacement();
            }

            if (target is IInteractableTrigger interactable)
                interactable.Interact(this);
        }

        private void OnRemoveTarget(ISensorTarget target)
        {
            var placement = target as Placement;
            if (placement != null)
            {
                _placements.Remove(placement);
                placement.StopInteraction(this);
            }
            
            if (placement == _currentPlacement)
            {
                if (_isUsingPlacement)
                    StopInteraction();
                _currentPlacement = null;
                _hasCurrentPlacement = false;
            }

            if (_placements.Count > 0)
            {
                _currentPlacement = _placements.Last();
                _hasCurrentPlacement = true;
                UpdateCurrentPlacement();
            }
        }

        private void StartInteraction()
        {
            if (!_hasCurrentPlacement)
                return;
            _isUsingPlacement = true;
            _currentPlacement.StartInteraction(this);
        }

        private void StopInteraction()
        {
            if (!_hasCurrentPlacement)
                return;
            _isUsingPlacement = false;
            _currentPlacement.StopInteraction(this);
        }
    }
}