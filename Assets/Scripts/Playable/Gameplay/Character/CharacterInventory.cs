using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Character
{
    public class CharacterInventory : MonoBehaviour
    {
        [SerializeField] private List<BaseCargo> _cargos = new List<BaseCargo>();
        [SerializeField] private float _extraDistanceInterval = 0.1f;

#if UNITY_EDITOR
        [SerializeField] private bool _debugCargoSorting;
#endif
        
        private Dictionary<int, BaseCargo> _cargoByItemId = new Dictionary<int, BaseCargo>();

        private void Awake()
        {
            foreach (var cargo in _cargos)
            {
                _cargoByItemId[cargo.ItemTypeId.Hash] = cargo;
                cargo.OnOccupySlot += OnOccupySlot;
                cargo.OnReleaseFromSlot += OnReleaseFromSlot;
            }
        }

        private void OnDestroy()
        {
            foreach (var cargo in _cargos)
            {
                cargo.OnOccupySlot -= OnOccupySlot;
                cargo.OnReleaseFromSlot -= OnReleaseFromSlot;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_debugCargoSorting)
                UpdateCargoSorting();
        }
#endif

        public bool TryGetCargoByIndex(int cargoIndex, out BaseCargo cargo)
        {
            cargo = null;
            if (cargoIndex < 0 || cargoIndex >= _cargos.Count)
                return false;

            cargo = _cargos[cargoIndex];
            return true;
        }

        public bool TryGetCargoByItemId(HashId itemId, out BaseCargo cargo)
        {
            return _cargoByItemId.TryGetValue(itemId.Hash, out cargo);
        }

        private void OnOccupySlot(Transform slot, Transform item)
        {
            UpdateCargoSorting();
        }

        private void OnReleaseFromSlot(Transform slot, Transform item)
        {
            UpdateCargoSorting();
        }

        private void UpdateCargoSorting()
        {
            if (_cargos.Count == 0)
                return;

            if (!GetNextBusyCargo(0, out var firstCargo))
                return;
            
            var offset = -firstCargo.Interval.z * firstCargo.Size.z * 0.5f + firstCargo.AdditionalPostInterval;
            
            for (int i = 0; i < _cargos.Count; i++)
            {
                var cargo = _cargos[i];
                var pos = cargo.transform.localPosition;
                pos.z = offset;
                cargo.transform.localPosition = pos;
                if (cargo.OccupiedSlotsCount > 0)
                {
                    offset -= cargo.Interval.z * cargo.Size.z * 0.5f + _extraDistanceInterval + cargo.AdditionalPostInterval;
                    if (GetNextBusyCargo(i + 1, out var nextCargo))
                        offset -= nextCargo.Interval.z * nextCargo.Size.z * 0.5f + nextCargo.AdditionalPreInterval;
                }
            }
        }

        private bool GetNextBusyCargo(int startIndex, out BaseCargo cargo)
        {
            for (int i = startIndex; i < _cargos.Count; i++)
            {
                if (_cargos[i].OccupiedSlotsCount > 0)
                {
                    cargo = _cargos[i];
                    return true;
                }
            }
            cargo = null;
            return false;
        }
    }
}