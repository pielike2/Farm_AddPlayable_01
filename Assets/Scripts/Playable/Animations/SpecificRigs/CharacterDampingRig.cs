using Playable.Gameplay.Character;
using UnityEngine;
using Utility;

namespace Playable.Animations
{
    public class CharacterDampingRig : MonoBehaviour
    {
        [SerializeField] private Transform _bone;
        [SerializeField] private SpringFloat _spring;
        [SerializeField] private float _startMovementBump = 100f;
        [SerializeField] private float _endMovementBump = -150f;
        
        private CharacterMovement _movement;
        private bool _prevMoving;

        private void Awake()
        {
            _movement = GetComponent<CharacterMovement>();
        }

        private void LateUpdate()
        {
            if (!_prevMoving && _movement.IsMoving)
                _spring.Velocity += _startMovementBump;
            else if (_prevMoving && !_movement.IsMoving)
                _spring.Velocity += _endMovementBump;
            
            _spring.UpdateSpringValue(Time.deltaTime);
            var springValue = _spring.CurrentValue;
            
            _bone.localRotation = Quaternion.Euler(0f, 0f, springValue) * _bone.localRotation;

            _prevMoving = _movement.IsMoving;
        }
    }
}