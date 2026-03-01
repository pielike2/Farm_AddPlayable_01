using System.Collections.Generic;
using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Character
{
    public class EquipZone : MonoBehaviour
    {
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] private HashId[] _targetSenseIds = new HashId[] {new HashId("MainCharacter")};
        [SerializeField] private int _toolIndex;
        [SerializeField] private bool _unequipOnly;

        private readonly Dictionary<IPlayerCharacter, int> _previousToolIndices = new Dictionary<IPlayerCharacter, int>();

        private void Awake()
        {
            for (int i = 0; i < _targetSenseIds.Length; i++)
            {
                _sensor.GetFilter(_targetSenseIds[i]).OnAddTarget += OnAddTarget;
                _sensor.GetFilter(_targetSenseIds[i]).OnRemoveTarget += OnRemoveTarget;
            }
        }

        private void OnAddTarget(ISensorTarget target)
        {
            var character = target.transform.GetComponent<IPlayerCharacter>();
            if (character == null)
                return;

            if (_unequipOnly)
            {
                _previousToolIndices[character] = character.CurrentStateId;
                character.DeactivateCustomState();
            }
            else
            {
                character.ActivateStateByIndex(_toolIndex);
            }
        }

        private void OnRemoveTarget(ISensorTarget target)
        {
            var character = target.transform.GetComponent<IPlayerCharacter>();
            if (character == null)
                return;

            if (_unequipOnly && _previousToolIndices.ContainsKey(character))
            {
                character.ActivateStateByIndex(_previousToolIndices[character]);
                _previousToolIndices.Remove(character);
            }
            else
            {
                character.DeactivateCustomState();
            }
        }
    }
}