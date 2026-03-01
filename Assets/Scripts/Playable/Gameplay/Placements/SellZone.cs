using System.Collections;
using Base;
using Base.PoolingSystem;
using Base.SignalSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Character;
using Playable.Gameplay.Loot;
using Playable.Gameplay.NPCs;
using UnityEngine;
using UnityEngine.Events;

namespace Playable.Gameplay.Placements
{
    public interface ISellPlacement
    {
        int MoneyPacksPerOperation { get; }
    }

    public class SellZone : MonoBehaviour, ISellPlacement
    {
        [SerializeField] private Placement _placement;
        [SerializeField] private BaseCargo _storageCargo;
        [SerializeField] private BaseCargo _moneyCargo;
        [SerializeField] private float _delayToStart = 0.1f;
        [SerializeField] private float _interval = 0.1f;
        [SerializeField] private BaseMovementTweenAnimation _itemMoveAnim;
        [SerializeField] private BaseSimpleTweenAnimation _moneyAppearAnim;
        [SerializeField] private LootItem _moneyPrefab;
        [SerializeField] private int _moneySpawnCount = 2;
        [SerializeField] private float _moneySpawnInterval = 0.2f;
        [SerializeField] private QueueController _queue;
        [SerializeField] private ScriptableSignal _signalOnOrderDone;
        [SerializeField] private AudioClipShell _sellSfx;

        public UnityEvent _onCompleteSell;
        public UnityEvent _onCompleteSellFull;
        
        private Coroutine _sellCoroutine;
        private WaitForSeconds _intervalWait;
        private WaitForSeconds _delayWait;
        private IInteractor _currentInteractor;

        public int MoneyPacksPerOperation => _moneySpawnCount;

        private void Reset()
        {
            _placement = GetComponent<Placement>();
        }

        private void Awake()
        {
            _placement.OnStartInteraction += OnStartInteraction;
            _placement.OnStopInteraction += OnStopInteraction;

            _delayWait = new WaitForSeconds(_delayToStart);
            _intervalWait = new WaitForSeconds(_interval);
        }

        public void OnStartInteraction(Placement placement, IInteractor interactor)
        {
            if (_currentInteractor != null) 
                return;
            _currentInteractor = interactor;
            
            if (_sellCoroutine != null)
                StopCoroutine(_sellCoroutine);

            _sellCoroutine = StartCoroutine(SellRoutine(interactor));
        }

        private void OnStopInteraction(Placement placement, IInteractor interactor)
        {
            if (interactor != _currentInteractor) 
                return;
            _currentInteractor = null;
            
            if (_sellCoroutine != null)
                StopCoroutine(_sellCoroutine);
        }

        private IEnumerator SellRoutine(IInteractor interactor)
        {
            yield return _delayWait;
            
            while (true)
            {
                if (_queue.PurchaseActive && _storageCargo.TryReleaseTopItem(out var item, out _))
                    StartCoroutine(HandleReleasedItem(item));
                while (!_queue.PurchaseActive)
                    yield return null;
                yield return _intervalWait;
            }
        }

        private IEnumerator HandleReleasedItem(Transform releasedItem)
        {
            var releasedLootItem = releasedItem.GetComponent<ILootItem>();
            if (releasedLootItem == null)
                yield break;
            
            _onCompleteSell?.Invoke();

            _storageCargo.ReleaseItemFromSlot(releasedItem);

            var buyerCargo = _queue.BuyerCharacter.Cargo;
            var buyerSlot = buyerCargo.OccupyNextSlot(releasedItem);
            
            releasedItem.DOKill(true);
            
            var seq = _itemMoveAnim.Animate(releasedItem, buyerSlot.position);
            seq.onComplete = () =>
            {
                buyerCargo.PutTransformIntoSlot(releasedItem, buyerSlot);
            };

            if (buyerCargo.IsFull)
            {
                _onCompleteSellFull?.Invoke();
                _sellSfx.Play();
                
                for (int i = 0; i < MoneyPacksPerOperation; i++)
                {
                    yield return new WaitForSeconds(_moneySpawnInterval);
                    
                    var moneyItem = _moneyPrefab.Spawn();
                    var isFull = _moneyCargo.IsFull;
                    var moneySlot = _moneyCargo.OccupyNextSlot(moneyItem.transform);
                    moneyItem.NextSourcePrefab = _moneyPrefab;
                    
                    var moneyItemNewTransform = !isFull 
                        ? _moneyCargo.PutTransformIntoSlot(moneyItem.transform, moneySlot) 
                        : moneyItem.transform;

                    if (isFull)
                    {
                        moneyItem.transform.SetParent(moneySlot);
                        moneyItem.transform.localPosition = Vector3.zero;
                        moneyItem.transform.localRotation = Quaternion.identity;
                    }
                    
                    _moneyAppearAnim.Animate(moneyItemNewTransform).OnComplete(() =>
                    {
                        if(isFull)
                            moneyItem.Release();
                    });
                }   
                
                if (_signalOnOrderDone)
                    _signalOnOrderDone.Trigger(this);
            }
        }
    }
}