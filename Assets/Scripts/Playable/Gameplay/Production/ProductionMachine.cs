using Base.PoolingSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Gameplay.Production
{
    public class ProductionMachine : MonoBehaviour, IProductionMachine
    {
        [SerializeField] private BaseCargo _storageCargo;
        [SerializeField] private BaseCargo _targetCargo;
        [SerializeField] private Animator _machineAnimator;
        [SerializeField] private BaseSimpleTweenAnimation _consumedItemAnim;
        [SerializeField] private BaseMovementTweenAnimation _conveyorItemAnim;
        [SerializeField] private BaseMovementTweenAnimation _conveyorEndItemAnim;
        [SerializeField] private Transform _conveyorPosFrom;
        [SerializeField] private Transform _conveyorPosTo;
        [SerializeField] private LootItem _productPrefab;
        [SerializeField] private ParticleSystem _productionFx;
        
        [SerializeField] private bool _isWorkingByDefault = true;
        [SerializeField] private float _consumeCooldown = 0.5f;
        [SerializeField] private float _productionDelay = 0.5f;
        [SerializeField] private float _firstConsumeDelay = 0.2f;
        [SerializeField] private int _consumedItemsCount = 2;
        [SerializeField] private int _itemsPerProductionPoint = 1;
        
        [SerializeField] private float _itemFallDuration = 0.3f;
        [SerializeField] private float _itemFallDelay = 0.15f;

        private int _productionPoints;
        private float _consumeCooldownTime;
        private float _productionCooldownTime;
        private bool _needFirstConsumeDelay;
        private Tween[] _fallAnimTweens;
        private int _activeFallTweensCount;
        
        private static readonly int Hash_IsProducing = Animator.StringToHash("IsProducing");

        public int ConsumedItemsCount => _consumedItemsCount;
        public bool IsMachineActive { get; private set; }

        private void Awake()
        {
            IsMachineActive = _isWorkingByDefault;
            _needFirstConsumeDelay = true;
            _storageCargo.OnOccupySlot += OnStorageCargoOccupySlot;
            _fallAnimTweens = new Tween[_storageCargo.Slots.Count];
        }

        private void Update()
        {
            if (IsMachineActive)
            {
                if (_storageCargo.OccupiedSlotsCount > 0)
                    TryConsume();
            }
            
            TryProduce();

            var isProducing = Time.time < _productionCooldownTime;
            _machineAnimator.SetBool(Hash_IsProducing, isProducing);
            
            if (isProducing && !_productionFx.isPlaying)
                _productionFx.Play();
            if (!isProducing && _productionFx.isPlaying)
                _productionFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void Toggle(bool active)
        {
            IsMachineActive = active;
        }

        private void OnStorageCargoOccupySlot(Transform slot, Transform item)
        {
            _storageCargo.CalculateFall();
        }

        private void TryConsume()
        {
            if (_storageCargo.OccupiedSlotsCount > 0 && _needFirstConsumeDelay && Time.time > _consumeCooldownTime)
            {
                _consumeCooldownTime = Time.time + _firstConsumeDelay;
                _needFirstConsumeDelay = false;
            }
            
            if (Time.time < _consumeCooldownTime || _storageCargo.OccupiedSlotsCount == 0)
                return;

            _consumeCooldownTime = Time.time + _consumeCooldown;

            var consumedItemsCount = 0;
            for (int i = 0; i < ConsumedItemsCount; i++)
            {
                if (!_storageCargo.TryReleaseBottomItem(out var item))
                    continue;

                consumedItemsCount++;
                
                var lootItem = item.GetComponent<ILootItem>();

                var seq = _consumedItemAnim.Animate(item.transform);
                seq.onComplete = () =>
                {
                    lootItem.Release();
                };
            }

            AnimateStorageCargoFall();
            
            if (_storageCargo.OccupiedSlotsCount == 0)
                _needFirstConsumeDelay = true;

            DOVirtual.DelayedCall(_productionDelay, () => _productionPoints += consumedItemsCount / _itemsPerProductionPoint);
        }

        // TODO implement as utility
        private void AnimateStorageCargoFall()
        {
            for (int i = 0; i < _activeFallTweensCount; i++)
                _fallAnimTweens[i].Kill();

            _activeFallTweensCount = 0;

            Transform item;
            for (var i = 0; i < _storageCargo.Items.Count; i++)
            {
                item = _storageCargo.Items[i];
                if (!_storageCargo.TryGetItemSlot(item, out var slot))
                    continue;
                
                item.transform.SetParent(slot);
                
                var fallTween = item.DOMove(slot.position, _itemFallDuration).SetEase(Ease.Linear);
                if (_itemFallDelay > 0f)
                    fallTween.SetDelay(_itemFallDelay);
                
                _fallAnimTweens[_activeFallTweensCount] = fallTween;
                _activeFallTweensCount++;
            }
        }

        private void TryProduce()
        {
            if (Time.time < _productionCooldownTime || _productionPoints <= 0)
                return;
            _productionCooldownTime = Time.time + _productionDelay;

            var product = _productPrefab.Spawn(_conveyorPosFrom.position, _conveyorPosFrom.rotation);
            product.NextSourcePrefab = _productPrefab;

            var seq = _conveyorItemAnim.Animate(product.transform, _conveyorPosTo.position);
            seq.onComplete = () => TransferItemToTargetCargo(product);
            
            _productionPoints--;
        }

        private void TransferItemToTargetCargo(LootItem item)
        {
            if (_targetCargo.IsFull)
            {
                item.Release();
                return;
            }
            
            var slot = _targetCargo.OccupyNextSlot(item.transform);
            var seq = _conveyorEndItemAnim.Animate(item.transform, slot);
            seq.onComplete = () =>
            {
                _targetCargo.PutTransformIntoSlot(item.transform, slot);
            };
        }
        
        public void API_Activate()
        {
            Toggle(true);
        }

        public void API_Deactivate()
        {
            Toggle(false);
        }
    }
}