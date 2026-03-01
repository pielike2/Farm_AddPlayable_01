using System;
using UnityEngine;

namespace Playable.Gameplay.Character
{
    public interface IPlayerCharacter
    {
        float MovementSpeed { get; }
        Vector3 DestinationPoint { get; }
        bool IsMoving { get; }
        bool IsMovementEnabled { get; }
        int CurrentStateId { get; }
        CharacterAutoMovementState AutoMovementState { get; }
        Transform DestinationTarget { get; }

        void StartMoveToPoint(Vector3 targetPose);
        void StartMoveToPoint(Vector3 targetPos, Action<bool> callback);
        void StopMovement();
        void SetCustomIdleRotation(Quaternion rotation);
        void SetCustomIdleEulerRotation(Vector3 euler);
        void CancelCustomIdleRotation();
        void ResetMovement();
        void ToggleMovement(bool active);

        void StartAction(int actionTypeId, bool asBooleanAction = true, params object[] args);
        void StopAction(int actionTypeId);
        void StopActions();
        
        void ActivateStateByIndex(int index);
        void ActivateStateByTypeId(int stateTypeId);
        void DeactivateCustomState();
    }
    
    public class PlayerCharacter : MonoBehaviour, IPlayerCharacter
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterStateController _stateController;
        [SerializeField] private CharacterMovement _characterMovement;
        [SerializeField] private Vector3 _castomRotation;

        private PlayerCharacterActionController _actionController;

        public float MovementSpeed => _characterMovement.MovementSpeed;
        public CharacterAutoMovementState AutoMovementState => _characterMovement.AutoMovementState;
        public Vector3 DestinationPoint => _characterMovement.DestinationPoint;
        public Transform DestinationTarget => _characterMovement.DestinationTarget;
        public bool IsMoving => _characterMovement.IsMoving;
        public bool IsMovementEnabled => _characterMovement.IsMovementEnabled;
        public int CurrentStateId => _stateController.ActiveStateIndex;

        private void Awake()
        {
            _actionController = new PlayerCharacterActionController(this, _animator);
        }

        public void StartMoveToPoint(Vector3 targetPos)
        {
            _characterMovement.StartMoveToPoint(targetPos);
        }

        public void StartMoveToPoint(Vector3 targetPos, Action<bool> callback)
        {
            _characterMovement.StartMoveToPoint(targetPos, callback);
        }

        public void StopMovement()
        {
            _characterMovement.StopMovement();
        }

        public void SetCustomIdleRotation(Quaternion rotation)
        {
            _characterMovement.SetCustomIdleRotation(rotation);
        }

        public void SetCustomIdleEulerRotation(Vector3 euler)
        {
            SetCustomIdleRotation(Quaternion.Euler(euler));
        }

        public void CancelCustomIdleRotation()
        {
            _characterMovement.CancelCustomRotation();
        }

        public void ResetMovement()
        {
            _characterMovement.ResetMovement();
        }

        public void ToggleMovement(bool active)
        {
            _characterMovement.ToggleMovement(active);
        }

        public void StartAction(int actionTypeId, bool asBooleanAction = true, params object[] args)
        {
            _actionController.StartAction(actionTypeId, asBooleanAction, args);
        }

        public void StopAction(int actionTypeId)
        {
            _actionController.StopAction(actionTypeId);
        }

        public void StopActions()
        {
            _actionController.StopActions();
        }

        public void ActivateStateByIndex(int index)
        {
            _stateController.ActivateStateByIndex(index);
        }

        public void ActivateStateByTypeId(int stateTypeId)
        {
            _stateController.ActivateStateByTypeId(stateTypeId);
        }

        public void DeactivateCustomState()
        {
            _stateController.DeactivateCustomState();
        }
    }
}