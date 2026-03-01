using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Placements
{
    public class PlacementHighlight : MonoBehaviour, IPlacementHighlight, ISensorTarget
    {
        [SerializeField] private HashId _senseId = new HashId("Interactable");
        [SerializeField] private QuadSprite _renderer;
        [SerializeField] private Texture2D _spritePassive;
        [SerializeField] private Texture2D _spriteActive;
        [SerializeField] private bool _skipFirstEnter;
        
        private Collider _collider;
        private int _countEnters;

        public HashId SenseId => _senseId;
        public bool IsColliderActive => _collider.enabled;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void ToggleHighlight(bool active)
        {
            if(_skipFirstEnter && _countEnters++ <= 0)
                return;
            
            _renderer.Texture = active ? _spriteActive : _spritePassive;
        }
    }
}