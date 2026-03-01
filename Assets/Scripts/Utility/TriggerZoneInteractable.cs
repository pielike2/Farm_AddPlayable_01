using Playable.Gameplay.Character;
using UnityEngine;
using UnityEngine.Events;
using Utility.SensorSystem;

namespace Utility
{
    public interface IInteractableTrigger
    {
        void Interact(IInteractor interactor);
    }

    public class TriggerZoneInteractable : MonoBehaviour, ISensorTarget, IInteractableTrigger
    {
        [SerializeField] private HashId _senseId;
        [SerializeField] private UnityEvent _onTriggerEnter;
        
        private Collider _collider;

        public HashId SenseId => _senseId;
        public bool IsColliderActive => _collider.enabled;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void Interact(IInteractor interactor)
        {
            _onTriggerEnter.Invoke();
        }
    }
}