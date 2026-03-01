using Base.PoolingSystem;
using Playable.Gameplay.NPCs;
using UnityEngine;

namespace Playable.UI
{
    public class QueueCharacterWidgetsContainer : MonoBehaviour
    {
        [SerializeField] private QueueController _queue;
        [SerializeField] private QueueCharacterWidget _characterWidgetPrefab;
        [SerializeField] private Transform _root;

        private void Awake()
        {
            if (_queue != null)
                _queue.OnCharacterSpawn += OnCharacterSpawn;
        }

        private void OnDestroy()
        {
            if (_queue != null)
                _queue.OnCharacterSpawn -= OnCharacterSpawn;
        }

        private void Reset()
        {
            _root = transform;
        }

        private void OnCharacterSpawn(QueueCharacter character)
        {
            var widget = _characterWidgetPrefab.Spawn();
            widget.transform.SetParent(_root, false);
            widget.SetCharacter(character);
            character.AddOnReleaseHandler(o => widget.Release());
        }
    }
}