using System;
using Base.PoolingSystem;
using UnityEditor;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.NPCs
{
    public enum NpcQueueState
    {
        InQueue,
        Buying,
        Leaving,
    }
    
    public class QueueCharacter : MonoBehaviour, IMonoPoolObject
    {
        [SerializeField] private BaseCargo _cargo;
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _useAnimatorReset;
        [SerializeField] private Animation _animation;
        [SerializeField] private float _movementSpeed = 5f;
        [SerializeField] private float _rotationLerpSpeed = 10f;
        [SerializeField] private Transform _widgetOrigin;

        [SerializeField] private Avatar _avatar;
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private CargoPosition _cargoPosition;
        
        private LinePath _path;

        private float _currentDistance;
        private float _prevDistance;
        private int _currentPoint;
        private bool _needStopAtDist;
        private float _stopDist;
        private Vector3 _stopPointLookDir;
        private bool _forceStop;
        private float _movementSpeedMult = 1f;
        private bool _noFirstSpawn;
        
        private static readonly int Hash_Moving = Animator.StringToHash("IsMoving");
        private static readonly int Hash_HandHold = Animator.StringToHash("HandHold");

        public int CurrentPoint => _currentPoint;
        public bool IsMoving { get; private set; }
        public bool IsOnDestinationPoint { get; private set; }
        public float DestinationArrivalTime { get; private set; }
        public float DestinationWaitTime => Time.time - DestinationArrivalTime;
        public NpcQueueState QueueState { get; set; }
        public float CurrentDistance => _currentDistance;
        public BaseCargo Cargo => _cargo;
        public Transform WidgetOrigin => _widgetOrigin;
        public float MovementSpeedMult
        {
            get => _movementSpeedMult;
            set => _movementSpeedMult = value;
        }

        private void Start()
        {
            PlayAnimation();
        }

        public void OnSpawnFromPool()
        {
            if (_useAnimatorReset && _noFirstSpawn && _animator != null)
            {
                DestroyImmediate(_animator);
                _animator = gameObject.AddComponent<Animator>();
                _animator.runtimeAnimatorController = _controller;
                _animator.avatar = _avatar;
                
                if(_cargoPosition != null)
                    _cargoPosition.Animator = _animator;
            }

            _noFirstSpawn = true;
            
            PlayAnimation();
        }

        public void OnReturnToPool()
        {
            _path = null;
            _currentDistance = 0f;
            _currentPoint = 0;
            _needStopAtDist = false;
            _forceStop = false;
            QueueState = default;

            foreach (var item in _cargo.Items)
                item.GetComponent<IMonoPoolObject>().Release();
            
            _cargo.FreeAllSlots();
            _cargo.ClearEvents();
        }

        public void SetPath(LinePath path)
        {
            _path = path;
        }

        public void StopAtDist(float dist, Vector3 lookDir)
        {
            _needStopAtDist = true;
            _stopDist = dist;
            _stopPointLookDir = lookDir;
        }

        public void CancelStopAtPoint()
        {
            _needStopAtDist = false;
        }

        private void Update()
        {
            if(_animator == null) return;
            _animator.SetBool(Hash_Moving, IsMoving);
            _animator.SetBool(Hash_HandHold, _cargo.OccupiedSlotsCount > 0);
        }

        public void UpdateMovement()
        {
            var wasOnDestinationPoint = IsOnDestinationPoint;
            
            var useDestination = _needStopAtDist && CurrentDistance >= _stopDist; 
            if (useDestination)
            {
                _currentDistance = _stopDist;
                IsMoving = false;
                IsOnDestinationPoint = true;
                if (!wasOnDestinationPoint)
                    DestinationArrivalTime = Time.time;
            }
            else
            {
                IsOnDestinationPoint = false;
                if (_forceStop)
                    IsMoving = _currentDistance != _prevDistance;
                else
                {
                    _currentDistance = CurrentDistance + _movementSpeed * _movementSpeedMult * Time.deltaTime;
                    IsMoving = true;
                }
            }
            
            _path.Sample(CurrentDistance, out var pos, out var dir, out var pointIndex);
            _currentPoint = pointIndex;
            
            transform.position = pos;

            var rotation = useDestination
                ? Quaternion.LookRotation(_stopPointLookDir, Vector3.up)
                : Quaternion.LookRotation(dir, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _rotationLerpSpeed * Time.deltaTime);

            _prevDistance = _currentDistance;
        }

        public void ForceSetDistance(float dist)
        {
            _currentDistance = dist;
        }

        public void ForceStop(bool stop)
        {
            _forceStop = stop;
        }

        private void PlayAnimation()
        {
            if (_animation != null)
            {
                _animation.Play();
            }
        }
    }
}