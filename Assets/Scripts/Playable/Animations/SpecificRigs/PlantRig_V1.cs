using Playable.Gameplay.Collectables;
using UnityEngine;
using Utility;
using Utility.SensorSystem;
using Object = UnityEngine.Object;

namespace Playable.Animations.SpecificRigs
{
    public class PlantRig_V1 : MonoBehaviour, ITickByDemand, ISensorTarget
    {
        [SerializeField] private BaseCollectable _collectable;
        [SerializeField] private HashId _senseId;
        
        [SerializeField] private SpringFloat _spring;
        [SerializeField] private Transform _animRoot;

        [SerializeField] private float _tiltRadius = 0.3f;
        [SerializeField] private float _tiltAngle = 10f;
        
        [SerializeField] private float _springBump = -10f;
        [SerializeField] private float _springNudge = -0.2f;
        [SerializeField] private float _springRollAngle = 10f;
        [SerializeField] private float _springDuration = 0.5f;
        
        private Quaternion _initialRotation;
        private float _hitTiltAngle;
        private float _idleTiltAngle;
        private float _hitAnimEndTime;
        private Vector3 _springScale;
        private bool _isTicking;
        private Collider _collider;
        private MarkedReference<Transform> _mainCharacterRef = new MarkedReference<Transform>(PlayableConstants.RefId_MainCharacter);

        public HashId SenseId => _senseId;
        public bool IsColliderActive => _collider.enabled;

        private void Awake()
        {
            _collectable.OnTakeDamage += OnTakeDamage;
            
            _collider = GetComponent<Collider>();
            
            _spring.SetInitialValue(1f);
            _spring.RestoreInitialValue();
        }

        private void Start()
        {
            _initialRotation = transform.rotation;
        }

        private void OnDestroy()
        {
            _collectable.OnTakeDamage -= OnTakeDamage;
        }

        private void OnTakeDamage(float damage, Object source)
        {
            if (!_isTicking)
                return;
            _hitAnimEndTime = Time.time + _springDuration;
            _spring.Bump(_springBump);
            _spring.CurrentValue += _springNudge;
        }
        
        public void Tick()
        {
            if (_mainCharacterRef.IsNull)
            {
                transform.rotation = _initialRotation;
                RestoreInitialSpring();
                return;
            }
            
            var mainCharPos = _mainCharacterRef.Value.position;
            var dir = transform.position - mainCharPos;
            var dist = dir.magnitude;
            var isCharacterNear = dist < _tiltRadius;
            var isHitAnimated = _isTicking && Time.time < _hitAnimEndTime;
            var needRotation = _isTicking && (isCharacterNear || isHitAnimated);
            
            if (isHitAnimated)
                UpdateSpring();
            else
                RestoreInitialSpring();
            
            if (needRotation)
            {
                var tiltAxis = Vector3.Cross(Vector3.up, dir / dist);
                if (isCharacterNear)
                    _idleTiltAngle = (1f - Mathf.Clamp01(dist / _tiltRadius)) * _tiltAngle;
                var tiltRotation = Quaternion.AngleAxis(_hitTiltAngle + _idleTiltAngle, tiltAxis);
                transform.rotation = tiltRotation * _initialRotation;
            }
            else
            {
                transform.rotation = _initialRotation;
            }
        }

        private void RestoreInitialSpring()
        {
            _spring.RestoreInitialValue();
            _spring.Velocity = 0f;
            transform.localScale = Vector3.one;
        }

        public void EnterTick()
        {
            _isTicking = true;
        }

        public void ExitTick()
        {
            _isTicking = false;
            
            _idleTiltAngle = 0f;
            _hitTiltAngle = 0f;
            Tick();
        }

        private void UpdateSpring()
        {
            _spring.UpdateSpringValue(Time.deltaTime);

            _springScale.x = 1f / _spring.CurrentValue;
            _springScale.y = (1f + _spring.CurrentValue) * 0.5f;
            _springScale.z = 1f / _spring.CurrentValue; 
            
            _animRoot.localScale = _springScale;
            _hitTiltAngle = (_spring.CurrentValue - 1f) * _springRollAngle;
        }
    }
}