using System.Collections;
using Base;
using Base.PoolingSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Character;
using Playable.Gameplay.Loot;
using Playable.Signals;
using UnityEngine;
using UnityEngine.Events;

namespace Playable.Gameplay.Placements
{
    public class ResourceCollectZone_V1 : MonoBehaviour
    {
        [SerializeField] private Placement _placement;
        [SerializeField] private BaseCargo _storageCargo;
        [SerializeField] private bool _cancelWhenFullTarget;
        [SerializeField] private bool _cancelWhenOut = true;
        [SerializeField] private bool _repeatInteraction = true;
        [SerializeField] private CollectFromCargoAnimation _collectAnimation;
        [SerializeField] private float _startCollectDelay = 0.5f;
        [SerializeField] private float _collectInterval = 0.03f;
        [SerializeField] private float _collectIntervalFactor = 0.9f;
        [SerializeField] private float _collectMinInterval = 0.01f;
        [SerializeField] private float _collectJumpDurationFactor = 0.5f;
        [SerializeField] private Transform _centerPoint;
        [SerializeField] private AudioClipShell _collectSfx;
        [SerializeField] private float _collectSfxCooldown = 0.05f;
        [SerializeField] private UnityEvent _onCollect;

        private bool _coroutineActive;
        private Coroutine _coroutine;
        private bool _isInteractionActive;
        private IInteractor _interactor;
        private bool _isStorageCargoValid;
        private BaseCargo _targetCargo;
        private float _collectSfxCooldownTime;

        private void Reset()
        {
            _placement = GetComponent<Placement>();
        }

        private void Awake()
        {
            _placement.OnStartInteraction += OnStartInteraction;
            _placement.OnStopInteraction += OnStopInteraction;
            _storageCargo.OnOccupySlot += OnOccupySlot;
            _isStorageCargoValid = _storageCargo != null;
        }

        private void OnOccupySlot(Transform slot, Transform item)
        {
            if (_isInteractionActive && _repeatInteraction && !_coroutineActive)
                _coroutine = StartCoroutine(CollectRoutine(_interactor, _targetCargo, false));
        }

        private void OnStartInteraction(Placement placement, IInteractor interactor)
        {
            if (!_isStorageCargoValid)
                return;

            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();
            if (inventory == null || !inventory.TryGetCargoByItemId(_storageCargo.ItemTypeId, out var cargo))
                return;

            _isInteractionActive = true;
            _targetCargo = cargo;
            _interactor = interactor;

            if (!_coroutineActive)
                _coroutine = StartCoroutine(CollectRoutine(interactor, cargo, true));
        }

        private void OnStopInteraction(Placement placement, IInteractor interactor)
        {
            if (_cancelWhenOut && _coroutineActive)
            {
                StopCoroutine(_coroutine);
                _coroutineActive = false;
            }

            _isInteractionActive = false;
            _interactor = null;
            _targetCargo = null;
        }

        private IEnumerator CollectRoutine(IInteractor interactor, BaseCargo targetCargo, bool onEnter)
        {
            _coroutineActive = true;
            if (!onEnter)
                yield return new WaitForSeconds(_startCollectDelay);

            var interval = _collectInterval;
            var jumpDurationMult = 1f;
            var nextIterationTime = Time.time;

            while (_storageCargo.OccupiedSlotsCount > 0)
            {
                if (Time.time < nextIterationTime)
                {
                    yield return null;
                    continue;
                }

                if (_cancelWhenFullTarget && targetCargo.IsFull)
                {
                    _coroutineActive = false;
                    Get.SignalBus.Publish(new SCargoIsFull(targetCargo));
                    yield break;
                }

                if (!_storageCargo.TryReleaseTopItem(out var receivedItem, out _))
                {
                    _coroutineActive = false;
                    yield break;
                }

                var receivedLootItem = receivedItem.GetComponent<ILootItem>();

                var targetWasFull = targetCargo.IsFull;
                var slot = targetCargo.OccupyNextSlot(receivedItem);

                receivedItem.DOKill(true);
                receivedItem.transform.localScale = Vector3.one;

                var seq = _collectAnimation.Animate(receivedItem, slot, _centerPoint.position, interactor.transform, jumpDurationMult);
                seq.onComplete = () =>
                {
                    if (targetWasFull)
                        receivedLootItem.Release();
                    else
                        targetCargo.PutTransformIntoSlot(receivedItem, slot);
                };

                seq.InsertCallback(seq.Duration() * 0.5f, () =>
                {
                    if (targetWasFull)
                        Get.SignalBus.Publish(new SCargoIsFull(targetCargo));
                    else
                    {
                        if (receivedLootItem.IsCurrencyItem && Currency.TryGetCurrency(receivedLootItem.ItemTypeId.Hash, out var currency))
                            currency.AddCurrencyAmount(currency.ValuePerItem);
                        _onCollect.Invoke();
                    }
                });

                TryPlayCollectSfx();

                nextIterationTime += interval;
                jumpDurationMult += _collectInterval * _collectJumpDurationFactor;
                interval *= _collectIntervalFactor;
                if (interval < _collectMinInterval)
                    interval = _collectMinInterval;
            }

            yield return null;

            _coroutineActive = false;
        }

        private void TryPlayCollectSfx()
        {
            if (Time.time < _collectSfxCooldownTime)
                return;
            _collectSfxCooldownTime = Time.time + _collectSfxCooldown;

            _collectSfx.Play();
        }
    }
}
