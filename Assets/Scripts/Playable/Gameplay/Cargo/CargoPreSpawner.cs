using Base.PoolingSystem;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Gameplay
{
    [RequireComponent(typeof(BaseCargo))]
    public class CargoPreSpawner : MonoBehaviour
    {
        [SerializeField] private int _initialItemCount = 50;
        [SerializeField] private LootItem _itemPrefab;
        [SerializeField] private bool _fillOnStart = true;

        private BaseCargo _cargo;

        private void Awake()
        {
            _cargo = GetComponent<BaseCargo>();
        }

        private void Start()
        {
            if (_fillOnStart)
                FillCargo();
        }

        public void FillCargo()
        {
            if (_cargo == null || _itemPrefab == null)
                return;

            var itemsToAdd = Mathf.Min(_initialItemCount, _cargo.Slots.Count - _cargo.OccupiedSlotsCount);

            for (int i = 0; i < itemsToAdd; i++)
            {
                var lootItem = _itemPrefab.Spawn();
                lootItem.NextSourcePrefab = _itemPrefab;
                var slot = _cargo.OccupyNextSlot(lootItem.transform);
                _cargo.PutTransformIntoSlot(lootItem.transform, slot);
            }
        }
    }
}