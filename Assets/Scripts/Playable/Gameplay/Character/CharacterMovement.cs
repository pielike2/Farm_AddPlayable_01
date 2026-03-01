using System;
using Base;
using Base.SignalSystem;
using UnityEngine;
using Utility.Extensions;

namespace Playable.Gameplay.Character
{
    public enum CharacterAutoMovementState
    {
        NonAuto,
        MovingToPoint,
        MovingToTarget,
    }
    
    public class CharacterMovement : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;

        [SerializeField] private Animator _animator;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private ScriptableSignal _signalOnFirstMove;
        [SerializeField] private AudioSource _walkSfx;
        
        private static readonly int Hash_IsMoving = Animator.StringToHash("IsMoving");

        private Vector3 _prevPos;
        private bool _firstMoveDone;
        private Vector3 _lookDir;
        private Action<bool> _movementCallback;
        private bool _isMovementEnabled = true;
        
        public bool IsMoving { get; private set; }
        public Vector3 MovementDir { get; private set; }
        public CharacterAutoMovementState AutoMovementState { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float VelocityMagnitude { get; private set; }
        public bool IsCustomIdleRotationActive { get; private set; }
        public Quaternion CustomIdleRotation { get; private set; }
        public float MovementSpeed => _speed;
        public Vector3 DestinationPoint { get; private set; }
        public Transform DestinationTarget { get; private set; }
        public bool IsMovementEnabled => _isMovementEnabled;

        private void Awake()
        {
            MovementDir = transform.forward;
            _prevPos = transform.position;
        }

        private void Update()
        {
            if (!IsMovementEnabled)
                return;
            
            var input = Get.Input.CharacterControlBlocker.IsActive 
                ? Vector2.zero 
                : Get.Input.MovementVector.normalized;
            
            if (AutoMovementState != CharacterAutoMovementState.NonAuto && (Mathf.Abs(input.x) > 0 || Mathf.Abs(input.y) > 0))
            {
                AutoMovementState = CharacterAutoMovementState.NonAuto;
            }
            
            if (Get.Input.CharacterControlBlocker.IsActive || AutoMovementState != CharacterAutoMovementState.NonAuto)
                UpdateAutoMovement();
            else
                UpdateInputMovement(input);
            
            UpdateRotation();
            
            _animator.SetBool(Hash_IsMoving, IsMoving);
            
            _walkSfx.Toggle(IsMoving);

            Velocity = transform.position - _prevPos;
            VelocityMagnitude = Velocity.magnitude;
            _prevPos = transform.position;
        }
        
        public void StartMoveToPoint(Vector3 position)
        {
            DestinationPoint = position;
            AutoMovementState = CharacterAutoMovementState.MovingToPoint;
        }

        public void StartMoveToPoint(Vector3 position, Action<bool> callback)
        {
            DestinationPoint = position;
            AutoMovementState = CharacterAutoMovementState.MovingToPoint;
            _movementCallback = callback;
        }

        public void SetCustomIdleRotation(Quaternion rotation)
        {
            IsCustomIdleRotationActive = true;
            CustomIdleRotation = rotation;
        }

        public void CancelCustomRotation()
        {
            IsCustomIdleRotationActive = false;
        }

        public void ToggleMovement(bool active)
        {
            _isMovementEnabled = active;
            if (!active)
                StopMovement();
        }

        public void StopMovement()
        {
            IsMoving = false;
            AutoMovementState = CharacterAutoMovementState.NonAuto;
            _movementCallback?.Invoke(false);
            _movementCallback = null;
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        public void ResetMovement()
        {
            AutoMovementState = CharacterAutoMovementState.NonAuto;
            IsCustomIdleRotationActive = false;
            IsMoving = false;
            MovementDir = Vector3.zero;
            CustomIdleRotation = Quaternion.identity;
            _movementCallback?.Invoke(false);
            _movementCallback = null;
        }

        private void UpdateAutoMovement()
        {
            if (AutoMovementState == CharacterAutoMovementState.NonAuto)
            {
                IsMoving = false;
                return;
            }

            Vector3 targetPos = Vector3.zero;
            switch (AutoMovementState)
            {
                case CharacterAutoMovementState.MovingToTarget:
                    targetPos = DestinationTarget.position;
                    break;
                case CharacterAutoMovementState.MovingToPoint:
                    targetPos = DestinationPoint;
                    break;
            }
            
            var dt = targetPos - transform.position;
            var dist = dt.magnitude;
            var maxMovementDist = _speed * Mathf.Min(Time.deltaTime, 0.05f);

            if (dist <= maxMovementDist)
            {
                transform.position = targetPos; 
                DestinationTarget = null;
                AutoMovementState = CharacterAutoMovementState.NonAuto;
                IsMoving = false;
                if (_movementCallback != null)
                {
                    _movementCallback.Invoke(true);
                    _movementCallback = null;
                }
                return;
            }
            
            IsMoving = true;
            MovementDir = dt / dist;
            var movement = MovementDir * maxMovementDist;
            _characterController.Move(movement, MovementDir);
        }

        private void UpdateInputMovement(Vector2 input)
        {
            var camTransform = Get.SceneVars.mainCamera.transform;
            
            // TODO optimize
            var camForward = camTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            var camRight = camTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            var movement = camForward * input.y + camRight * input.x;
            var movementMagnitude = movement.magnitude;
            if (movementMagnitude > 0f)
                MovementDir = movement / movementMagnitude;

            IsMoving = movementMagnitude > 0.001f;
            
            _characterController.Move(movement * _speed * Mathf.Min(Time.deltaTime, 0.1f), MovementDir);

            if (!_firstMoveDone && movementMagnitude > 0f)
            {
                _firstMoveDone = true;
                if (_signalOnFirstMove)
                    _signalOnFirstMove.Trigger(this);
            }
        }
        
        private void UpdateRotation()
        {
            if (IsMoving)
            {
                _lookDir = MovementDir;
                _lookDir.y = 0f;
                transform.rotation = LerpRotation(Quaternion.LookRotation(_lookDir));    
            }
            else if (IsCustomIdleRotationActive)
            {
                transform.rotation = LerpRotation(CustomIdleRotation);
            }
        }
        
        private Quaternion LerpRotation(Quaternion targetRotation)
        {
            return Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }
}