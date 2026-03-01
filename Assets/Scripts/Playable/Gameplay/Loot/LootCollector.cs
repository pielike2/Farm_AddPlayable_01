using Base;
using Base.PoolingSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Collectables;
using Playable.Signals;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Loot
{
    public class LootCollector : MonoBehaviour, ILootCollector
    {
        [SerializeField] private Transform _lootCollectPoint;
        [SerializeField] private BaseLootCollectAnimation _defaultCollectAnimation;
        [SerializeField] private BaseSimpleTweenAnimation _cargoItemAppearAnimation;
        [SerializeField] private int _cargoItemAnimationCountThreshold = 30;
        [SerializeField] private float _cargoItemScale = 0.8f;
        [SerializeField] private BaseCargo _cargo;
        [SerializeField] private bool _useMultipleCargos;
        [SerializeField] private BaseCargo[] _cargos = {};
        [SerializeField] private AudioClipShell _collectSfx;
        [SerializeField] private int _overrideCollectCount = -1;
        
        [Space(10)] 
        [SerializeField] private bool _useFlyToCargo;
        [SerializeField] private float _flyToCargoDuration = 0.3f;
        [SerializeField] private float _flyToCargoEndScale = 1f;

        private int _currentCargoIndex;
        
        public bool CanCollect => true;
        public int LootCollectPointsCount => 1;
        public int OverrideCollectCount => _overrideCollectCount;
        
        public Transform GetLootCollectPoint(int index)
        {
            return _lootCollectPoint;
        }

        public void CollectLoot(ILootItem lootItem, ICollectable collectable, int i)
        {
            var seq = _defaultCollectAnimation.Animate(lootItem, collectable, this);

            var lootItemPos = lootItem.transform.position;
            var lootItemRot = lootItem.transform.rotation;
            
            var nextPrefab = lootItem.NextSourcePrefab;
            seq.onComplete = lootItem.Release;
            
            var cargo = _useMultipleCargos ? _cargos[_currentCargoIndex] : _cargo;

            _currentCargoIndex++;
            if (_currentCargoIndex >= _cargos.Length)
                _currentCargoIndex = 0;

            seq.InsertCallback(_defaultCollectAnimation.customCallbackTimes[0], () =>
            {
                if (cargo.IsFull)
                    return;

                var cargoItem = nextPrefab.Spawn();
                cargoItem.NextSourcePrefab = nextPrefab;
                
                var slot = cargo.OccupyNextSlot(cargoItem.transform);
                var cargoItemNewTransform = cargo.PutTransformIntoSlot(cargoItem.transform, slot);
                
                if (_useFlyToCargo)
                {
                    cargoItemNewTransform.position = lootItemPos;
                    cargoItemNewTransform.rotation = lootItemRot;
                    cargoItemNewTransform.DOLocalMove(Vector3.zero, _flyToCargoDuration).SetEase(Ease.Linear);
                    cargoItemNewTransform.DOLocalRotate(Vector3.zero, _flyToCargoDuration).SetEase(Ease.Linear);
                    cargoItemNewTransform.DOScale(cargoItemNewTransform.localScale * _flyToCargoEndScale,  _flyToCargoEndScale).SetEase(Ease.Linear);
                }
                
                cargoItemNewTransform.localScale = Vector3.one * _cargoItemScale;
                if (cargo.OccupiedSlotsCount <= _cargoItemAnimationCountThreshold)
                    _cargoItemAppearAnimation.Animate(cargoItemNewTransform);
            });
                
            _collectSfx.Play();
            
            if (cargo.IsFull)
                Get.SignalBus.Publish(new SCargoIsFull(cargo));
        }
    }
}