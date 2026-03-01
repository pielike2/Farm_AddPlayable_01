using System.Collections;
using Base;
using Playable.Gameplay.Character;
using UnityEngine;
using UnityEngine.Events;

namespace Playable.Gameplay.Placements
{
    public class PlacementAutoInteractor : MonoBehaviour
    {
        public enum InteractionState
        {
            None,
            MovingToPoint,
            Rotating,
            PerformingAction,
            Interrupted
        }

        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private int _actionAnimationId;
        [SerializeField] private float _arrivalThreshold = 0.1f;
        [SerializeField] private float _inputCheckThreshold = 0.1f;

        [Space(10)]
        [SerializeField, Tooltip("Задержка в секундах перед началом движения к точке после срабатывания события")]
        private float _startDelay = 0f;

        [Space(10)]
        [SerializeField, Tooltip("Если выключено — игрок игнорируется при входе в зону")]
        private bool _usableByPlayer = true;

        [Space(10)] [Header("Events")] 
        public UnityEvent OnActionStarted;
        public UnityEvent OnActionInterrupted;

        private Placement _targetPlacement;
        private IPlayerCharacter _playerCharacter;
        private Coroutine _interactionCoroutine;
        private bool _wasInputActive;
        private bool _isCharacterInInteractionZone;
        private bool _isControllingCharacter;
        private IInteractor _currentInteractor;
        private bool _isExternallyControlled;

        public InteractionState CurrentState { get; private set; }
        public bool IsBusy => _currentInteractor != null;

        private void Awake()
        {
            if (_interactionPoint == null)
                _interactionPoint = transform;
        }

        private void Start()
        {
            _targetPlacement = GetComponent<Placement>();

            if (_targetPlacement == null)
                return;

            _targetPlacement.OnStartInteraction += OnPlacementInteractionStart;
            _targetPlacement.OnStopInteraction += OnPlacementInteractionStop;
        }

        private void Update()
        {
            CheckForInputInterruption();
        }

        private void OnDestroy()
        {
            if (_targetPlacement == null) return;
            _targetPlacement.OnStartInteraction -= OnPlacementInteractionStart;
            _targetPlacement.OnStopInteraction -= OnPlacementInteractionStop;
            
            if (IsBusy)
                ForceInterruptAndRelease();
        }
        
        public bool TryBeginExternalInteraction(IInteractor interactor)
        {
            if (interactor == null)
                return false;

            if (IsBusy && _currentInteractor != interactor)
                return false;

            if (CurrentState != InteractionState.None)
                InterruptAction();

            _currentInteractor = interactor;
            _isExternallyControlled = true;
            _isCharacterInInteractionZone = true;
            
            CurrentState = InteractionState.PerformingAction;
            OnActionStarted?.Invoke();

            return true;
        }
        
        public void EndExternalInteraction(IInteractor interactor)
        {
            if (_currentInteractor != interactor)
                return;

            _isCharacterInInteractionZone = false;
            InterruptAction();
        }

        private void OnPlacementInteractionStart(Placement placement, IInteractor interactor)
        {
            if (!_usableByPlayer)
                return;
            
            if (IsBusy && _currentInteractor != interactor)
                return;

            _playerCharacter = interactor.gameObject.GetComponent<IPlayerCharacter>();
            if (_playerCharacter == null)
                return;

            _currentInteractor ??= interactor;
            _isExternallyControlled = false;
            _isCharacterInInteractionZone = true;

            if (CurrentState != InteractionState.None)
                InterruptAction();

            _targetPlacement = placement;
            StartAutoInteraction();
        }

        private void OnPlacementInteractionStop(Placement placement, IInteractor interactor)
        {
            if (_targetPlacement == placement && _currentInteractor == interactor)
            {
                _isCharacterInInteractionZone = false;
                InterruptAction();
            }
        }

        private void StartAutoInteraction()
        {
            if (_interactionCoroutine != null)
                StopCoroutine(_interactionCoroutine);

            TakeCharacterControl();
            _interactionCoroutine = StartCoroutine(AutoInteractionSequence());
        }

        private void TakeCharacterControl()
        {
            if (_isControllingCharacter)
                return;

            _isControllingCharacter = true;
        }

        private IEnumerator AutoInteractionSequence()
        {
            yield return StartCoroutine(WaitForStartDelay());
            if (CurrentState == InteractionState.Interrupted || !_isCharacterInInteractionZone)
                yield break;

            yield return StartCoroutine(MoveToInteractionPoint());
            if (CurrentState == InteractionState.Interrupted)
                yield break;

            yield return StartCoroutine(RotateToTargetDirection());
            if (CurrentState == InteractionState.Interrupted)
                yield break;

            StartPerformingAction();
        }

        private IEnumerator WaitForStartDelay()
        {
            if (_startDelay <= 0f)
                yield break;

            var waitTime = 0f;
            while (waitTime < _startDelay)
            {
                if (CurrentState == InteractionState.Interrupted || !_isCharacterInInteractionZone)
                    yield break;

                waitTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator MoveToInteractionPoint()
        {
            if (_interactionPoint == null || _playerCharacter == null)
                yield break;

            var characterTransform = (_playerCharacter as MonoBehaviour).transform;
            var distanceToPoint = Vector3.Distance(characterTransform.position, _interactionPoint.position);
            if (distanceToPoint <= _arrivalThreshold)
                yield break;

            CurrentState = InteractionState.MovingToPoint;

            _playerCharacter.StartMoveToPoint(_interactionPoint.position);

            float distance;

            while (CurrentState == InteractionState.MovingToPoint)
            {
                distance = Vector3.Distance(characterTransform.position, _interactionPoint.position);
                if (distance <= _arrivalThreshold)
                {
                    _playerCharacter.StopMovement();
                    break;
                }

                yield return null;

                if (CurrentState == InteractionState.Interrupted)
                {
                    _playerCharacter.StopMovement();
                    yield break;
                }
            }
        }

        private IEnumerator RotateToTargetDirection()
        {
            if (_interactionPoint == null || _playerCharacter == null)
                yield break;

            CurrentState = InteractionState.Rotating;
            var targetRotation = Quaternion.LookRotation(_interactionPoint.forward);

            _playerCharacter.SetCustomIdleRotation(targetRotation);

            var minRotationTime = 0.4f;
            var rotationStartTime = Time.time;

            while (Time.time - rotationStartTime < minRotationTime && CurrentState == InteractionState.Rotating)
            {
                yield return null;

                if (CurrentState == InteractionState.Interrupted)
                    yield break;
            }
        }

        private void StartPerformingAction()
        {
            if (_playerCharacter == null)
                return;

            CurrentState = InteractionState.PerformingAction;
            _playerCharacter.StartAction(_actionAnimationId);
            OnActionStarted?.Invoke();
        }

        private void CheckForInputInterruption()
        {
            if (_isExternallyControlled)
                return;

            var hasInput = Get.Input.MovementVector.magnitude > _inputCheckThreshold;

            if (CurrentState != InteractionState.None)
            {
                if (hasInput && !_wasInputActive)
                {
                    InterruptAction();
                }
            }
            else if (_isCharacterInInteractionZone && _wasInputActive && !hasInput && _targetPlacement != null)
            {
                StartAutoInteraction();
            }

            _wasInputActive = hasInput;
        }

        private void InterruptAction()
        {
            if (CurrentState == InteractionState.None)
                return;

            CurrentState = InteractionState.Interrupted;

            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
            }

            if (_playerCharacter != null)
            {
                _playerCharacter.StopMovement();
                _playerCharacter.StopActions();
                _playerCharacter.CancelCustomIdleRotation();
            }

            OnActionInterrupted?.Invoke();

            CurrentState = InteractionState.None;

            if (!_isCharacterInInteractionZone)
            {
                _targetPlacement = null;
                _playerCharacter = null;
                _currentInteractor = null;
                _isExternallyControlled = false;
            }
        }

        private void ForceInterruptAndRelease()
        {
            _isCharacterInInteractionZone = false;
            InterruptAction();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_interactionPoint == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_interactionPoint.position, _arrivalThreshold);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_interactionPoint.position, _interactionPoint.position + _interactionPoint.forward * 1f);

            if (Application.isPlaying && _playerCharacter != null && _playerCharacter is MonoBehaviour characterMono)
            {
                var characterTransform = characterMono.transform;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(characterTransform.position, _interactionPoint.position);

                if (CurrentState == InteractionState.MovingToPoint)
                {
                    Gizmos.color = Color.red;
                    var direction = (_interactionPoint.position - characterTransform.position).normalized;
                    Gizmos.DrawLine(characterTransform.position, characterTransform.position + direction);
                }
                else if (CurrentState == InteractionState.Rotating)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(characterTransform.position,
                        characterTransform.position + characterTransform.forward * 1.5f);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(characterTransform.position,
                        characterTransform.position + _interactionPoint.forward * 1.8f);
                }
            }
        }
#endif
    }
}