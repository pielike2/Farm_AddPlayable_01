using System.Collections;
using Base;
using Base.PoolingSystem;
using Base.SignalSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Loot;
using Playable.Gameplay.NPCs;
using UnityEngine;

namespace Playable.Gameplay.Placements
{
    public class AutoSell : MonoBehaviour, ISellPlacement
    {
        [SerializeField] private BaseCargo _storageCargo;
        [SerializeField] private BaseCargo _moneyCargo;
        [SerializeField] private float _interval = 0.1f;
        [SerializeField] private BaseMovementTweenAnimation _itemMoveAnim;
        [SerializeField] private BaseSimpleTweenAnimation _moneyAppearAnim;
        [SerializeField] private LootItem _moneyPrefab;
        [SerializeField] private int _moneySpawnCount = 2;
        [SerializeField] private float _moneySpawnInterval = 0.2f;
        [SerializeField] private QueueController _queue;
        [SerializeField] private float _startSellDelay = 1f;
        [SerializeField] private ScriptableSignal _signalOnOrderDone;
        [SerializeField] private AudioClipShell _sellSfx;

        private Coroutine _sellCoroutine;
        private WaitForSeconds _intervalWait;

        public int MoneyPacksPerOperation => _moneySpawnCount;

        private void Awake()
        {
            _intervalWait = new WaitForSeconds(_interval);

            if (_storageCargo != null)
                _storageCargo.OnOccupySlot += OnStorageOccupySlot;
        }

        private void OnDestroy()
        {
            if (_storageCargo != null)
                _storageCargo.OnOccupySlot -= OnStorageOccupySlot;
        }

        private void OnStorageOccupySlot(Transform slot, Transform item)
        {
            TryStartSelling();
        }

        public void TryStartSelling()
        {
            if (_sellCoroutine == null)
                _sellCoroutine = StartCoroutine(SellRoutine());
        }

        public void StopSelling()
        {
            if (_sellCoroutine != null)
                StopCoroutine(_sellCoroutine);
            _sellCoroutine = null;
        }

        private IEnumerator SellRoutine()
        {
            yield return new WaitForSeconds(_startSellDelay);
            while (true)
            {
                if (_queue != null && _queue.PurchaseActive && !_queue.BuyerCharacter.Cargo.IsFull && _storageCargo.TryReleaseTopItem(out var item, out _))
                    StartCoroutine(HandleReleasedItem(item));

                if (_storageCargo.OccupiedSlotsCount == 0)
                    StopSelling();

                while (_queue == null || !_queue.PurchaseActive)
                    yield return null;
                yield return _intervalWait;
            }
        }

        private IEnumerator HandleReleasedItem(Transform releasedItem)
        {
            var releasedLootItem = releasedItem.GetComponent<ILootItem>();
            if (releasedLootItem == null)
                yield break;

            _storageCargo.ReleaseItemFromSlot(releasedItem);

            var buyerCargo = _queue.BuyerCharacter.Cargo;
            var buyerSlot = buyerCargo.OccupyNextSlot(releasedItem);

            releasedItem.DOKill();

            var seq = _itemMoveAnim.Animate(releasedItem, buyerSlot);
            seq.onComplete = () =>
            {
                releasedItem.transform.localScale = Vector3.one;
                buyerCargo.PutTransformIntoSlot(releasedItem, buyerSlot);
            };

            if (buyerCargo.IsFull)
            {
                for (int i = 0; i < MoneyPacksPerOperation; i++)
                {
                    yield return new WaitForSeconds(_moneySpawnInterval);

                    var moneyItem = _moneyPrefab.Spawn();
                    var moneySlot = _moneyCargo.OccupyNextSlot(moneyItem.transform);
                    moneyItem.NextSourcePrefab = _moneyPrefab;
                    var moneyItemNewTransform = _moneyCargo.PutTransformIntoSlot(moneyItem.transform, moneySlot);
                    _moneyAppearAnim.Animate(moneyItemNewTransform);
                }

                if (_signalOnOrderDone)
                    _signalOnOrderDone.Trigger(this);

                _sellSfx.TryPlayByDistance(transform.position);
            }
        }
    }
}
