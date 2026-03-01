using System.Collections;
using System.Collections.Generic;
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
    public class ResourceCollectZone : MonoBehaviour
    {
        [SerializeField] private Placement _placement;
        [SerializeField] private BaseCargo _storageCargo;
        [SerializeField] private bool _cancelWhenFullTarget;
        [SerializeField] private BaseMovementTweenAnimation _itemMoveAnim;
        [SerializeField] private float _startCollectDelay = 0.5f;
        [SerializeField] private float _collectInterval = 0.05f;
        [SerializeField] private AudioClipShell _collectSfx;
        [SerializeField] private float _cargoItemScale = 1f;
        [SerializeField] private UnityEvent _onCollect;

        private WaitForSeconds _intervalWait;
        private Coroutine _coroutine;
        private bool _isInteractionActive;
        
        private List<BaseCargo> _cargos = new List<BaseCargo>();

        private void Reset()
        {
            _placement = GetComponent<Placement>();
        }

        private void Awake()
        {
            _placement.OnStartInteraction += OnStartInteraction;
            _placement.OnStopInteraction += OnStopInteraction;
            _intervalWait = new WaitForSeconds(_collectInterval);
            _storageCargo.OnOccupySlot += OnOccupySlot;
        }

        private void OnOccupySlot(Transform slot, Transform item)
        {
            if (_isInteractionActive && _coroutine == null)
                _coroutine = StartCoroutine(CollectRoutine(false));
        }

        private void OnStartInteraction(Placement placement, IInteractor interactor)
        {
            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();

            if (inventory == null || !inventory.TryGetCargoByItemId(_storageCargo.ItemTypeId, out var cargo))
                return;

            if (!_cargos.Contains(cargo))
                _cargos.Add(cargo);
            
            _isInteractionActive = true;

            if (_coroutine == null)
                _coroutine = StartCoroutine(CollectRoutine(true));
        }

        private void OnStopInteraction(Placement placement, IInteractor interactor)
        {
            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();
            if (inventory.TryGetCargoByItemId(_storageCargo.ItemTypeId, out var sourceCargo))
            {
                _cargos.Remove(sourceCargo);
            }

            if (_coroutine != null && _cargos.Count == 0)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
                _isInteractionActive = false;
            }
        }

        private IEnumerator CollectRoutine(bool onEnter)
        {
            if (!onEnter)
                yield return new WaitForSeconds(_startCollectDelay);
            
            if (!_isInteractionActive)
            {
                _coroutine = null;
                yield break;
            }
            
            while (_storageCargo.OccupiedSlotsCount > 0 && _cargos.Count > 0)
            {
                for (var i = 0; i < _cargos.Count; i++)
                {
                    if (_cancelWhenFullTarget && _cargos[i].IsFull)
                    {
                        Get.SignalBus.Publish(new SCargoIsFull(_cargos[i]));
                        yield return null;
                        continue;
                    }
                    
                    if (_storageCargo.TryReleaseTopItem(out var item, out _))
                        StartCoroutine(HandleReceivedItem(item, _cargos[i]));
                }
                
                yield return _intervalWait;
            }

            yield return null;
            
            _coroutine = null;
        }

        private IEnumerator HandleReceivedItem(Transform receivedItem, BaseCargo targetCargo)
        {
            var receivedLootItem = receivedItem.GetComponent<ILootItem>();
            if (receivedLootItem == null)
                yield break;

            _storageCargo.ReleaseItemFromSlot(receivedItem);

            var targetWasFull = targetCargo.IsFull;
            var targetSlot = targetCargo.OccupyNextSlot(receivedItem);
            
            receivedItem.DOKill(true);
            receivedItem.SetParent(targetSlot);
            receivedItem.localScale = Vector3.one * _cargoItemScale;
            
            var seq = _itemMoveAnim.Animate(receivedItem, targetSlot);
            seq.Join(receivedItem.DOLocalRotate(Vector3.zero, seq.Duration()).SetEase(Ease.InCubic));
            seq.onComplete = () =>
            {
                if (!targetWasFull)
                    targetCargo.PutTransformIntoSlot(receivedItem, targetSlot);
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
                
                _collectSfx.Play();
            });
        }
    }
}