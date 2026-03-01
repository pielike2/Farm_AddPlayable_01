using Base;
using Base.SignalSystem;
using Playable.Animations;
using Playable.Gameplay.Loot;
using Playable.Signals;
using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Character
{
    public class CharacterLootPickuper : MonoBehaviour
    {
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] private HashId[] _senseIds = new []{new HashId("LootItem")};

        [SerializeField] private CharacterInventory _inventory;

        [Space(10)]
        [SerializeField] private BaseMovementTweenAnimation _movementAnim;
        
        [Space(10)]
        [SerializeField] private ScriptableSignal _signalOnPickup;
        
        [SerializeField] private AudioClipShell _collectSfx;
        [SerializeField] private Transform _rootPoint;

        private SensorFilter[] _sensorFilters;
        private bool _isPickupSignalValid;

        private void Awake()
        {
            _isPickupSignalValid = _signalOnPickup != null;
            _sensorFilters = new SensorFilter[_senseIds.Length];
            for (var i = 0; i < _senseIds.Length; i++)
            {
                _sensorFilters[i] = _sensor.GetFilter(_senseIds[i]);
                _sensorFilters[i].OnAddTarget += OnAddTarget;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _sensorFilters.Length; i++)
                _sensorFilters[i].Dispose();
        }

        private void OnAddTarget(ISensorTarget target)
        {
            var lootItem = target.gameObject.GetComponent<ILootItem>();
            if (lootItem != null)
                Pickup(lootItem);
        }

        private void Pickup(ILootItem lootItem)
        {
            if (!_inventory.TryGetCargoByItemId(lootItem.ItemTypeId, out var cargo))
                return;

            if (cargo.IsFull)
            {
                Get.SignalBus.Publish(new SCargoIsFull(cargo));
                return;
            }

            if (lootItem.WasPickedUpOnce || !lootItem.TryPickup())
                return;
            
            if(_isPickupSignalValid)
                _signalOnPickup.Trigger(this);
            
            var slot = cargo.OccupyNextSlot(lootItem.transform);
            _movementAnim.Animate(lootItem.transform, slot).onComplete = () =>
            {
                cargo.PutTransformIntoSlot(lootItem.transform, slot);
                _collectSfx.Play();
            };
        }
    }
}