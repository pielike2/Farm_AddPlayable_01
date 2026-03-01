using System;
using Base;
using Base.PoolingSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Character;
using Playable.Gameplay.Collectables;
using Playable.Signals;
using UnityEngine;

namespace Playable.Gameplay.Loot
{
    public class PlayerLootCollector : MonoBehaviour, ILootCollector
    {
        [SerializeField] private Transform _lootCollectPoint;
        [SerializeField] private DefaultCargoLootUtilitySettings _defaultCargoLootSettings;

        [Serializable]
        private class DefaultCargoLootUtilitySettings
        {
            public BaseLootCollectAnimation collectAnimation;
            public BaseSimpleTweenAnimation itemAppearAnimation;
            public int cargoItemAnimationCountThreshold = 30;
            public float cargoItemScale = 0.8f;
            public AudioClipShell collectSfx;
        }

        private bool _isDefaultSfxValid;
        private CharacterInventory _inventory;
        
        public bool CanCollect => true;
        public int LootCollectPointsCount => 1;
        public int OverrideCollectCount => -1;

        private void Awake()
        {
            _isDefaultSfxValid = _defaultCargoLootSettings.collectSfx.clip != null;
            _inventory = GetComponent<CharacterInventory>();
        }
        
        public Transform GetLootCollectPoint(int index)
        {
            return _lootCollectPoint;
        }

        public void CollectLoot(ILootItem lootItem, ICollectable collectable, int i)
        {
            var foundCargo = _inventory.TryGetCargoByItemId(lootItem.ItemTypeId, out var cargo);
            
            var isCargoLootUtility = false;
            CargoLootUtility cargoLootUtility = null;
            
            if (foundCargo)
            {
                cargoLootUtility = cargo.GetComponent<CargoLootUtility>();
                isCargoLootUtility = cargoLootUtility != null;
            }

            var collectAnim = isCargoLootUtility
                ? cargoLootUtility.CollectAnimation
                : _defaultCargoLootSettings.collectAnimation;

            var appearAnim = isCargoLootUtility
                ? cargoLootUtility.ItemAppearAnimation
                : _defaultCargoLootSettings.itemAppearAnimation;
            
            var seq = collectAnim.Animate(lootItem, collectable, this);

            var nextPrefab = lootItem.NextSourcePrefab;
            seq.onComplete = lootItem.Release;

            if (!foundCargo)
                return;

            seq.InsertCallback(collectAnim.customCallbackTimes[0], () =>
            {
                if (cargo.IsFull)
                    return;

                var cargoItem = nextPrefab.Spawn();
                cargoItem.NextSourcePrefab = nextPrefab;
                
                var slot = cargo.OccupyNextSlot(cargoItem.transform);
                var cargoItemNewTransform = cargo.PutTransformIntoSlot(cargoItem.transform, slot);

                if (isCargoLootUtility)
                    cargoItemNewTransform.localScale = Vector3.one * cargoLootUtility.CargoItemScale;
                else
                    cargoItemNewTransform.localScale = Vector3.one * _defaultCargoLootSettings.cargoItemScale;

                var animationThreshold = isCargoLootUtility
                    ? cargoLootUtility.CargoItemAnimationCountThreshold
                    : _defaultCargoLootSettings.cargoItemAnimationCountThreshold;
                
                if (cargo.OccupiedSlotsCount <= animationThreshold)
                    appearAnim.Animate(cargoItemNewTransform);
            });

            if (isCargoLootUtility)
            {
                if (cargoLootUtility.CollectSfx.clip != null)
                    cargoLootUtility.CollectSfx.Play();
            }
            else if (_isDefaultSfxValid)
            {
                _defaultCargoLootSettings.collectSfx.Play();
            }
            
            if (cargo.IsFull)
                Get.SignalBus.Publish(new SCargoIsFull(cargo));
        }
    }
}