using Base.PoolingSystem;
using Playable.Gameplay.NPCs;
using UnityEngine;
using UnityEngine.UI;
using Utility;
using Utility.Extensions;

namespace Playable.UI
{
    public class QueueCharacterWidget : BaseScreenConstraintWidget, IMonoPoolObject
    {
        [SerializeField] private GameObject _passiveState;
        [SerializeField] private GameObject _activeState;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Vector2 _fillRemap = new Vector2(0f, 1f);
        [SerializeField] private ImageNumberWidget _numberWidget;
        [SerializeField] private float _stateChangeDelay = 0.1f;
        [SerializeField] private float _numberMultiplier = 1f;

        private QueueCharacter _character;
        private NpcQueueState _prevState;
        private float _countIncTime;
        
        private int _dirtyCount;
        private int _displayedCount;

        public override Transform OriginPoint => _character.WidgetOrigin;

        protected override void Update()
        {
            base.Update();
            
            if (_prevState != _character.QueueState)
                RefreshState();
            _prevState = _character.QueueState;

            UpdateDisplayedCount();
        }

        private void UpdateDisplayedCount()
        {
            if (_dirtyCount <= 0 || Time.time < _countIncTime) 
                return;
            _countIncTime = Time.time + _stateChangeDelay;
            _dirtyCount--;
            _displayedCount++;
            RefreshState();
        }

        public void OnSpawnFromPool()
        {
        }

        public void OnReturnToPool()
        {
            _dirtyCount = 0;
            _displayedCount = 0;
            _prevState = NpcQueueState.InQueue;
        }

        public void SetCharacter(QueueCharacter character)
        {
            _character = character;
            RefreshState();
            UpdateScreenConstraint();
            _character.Cargo.OnOccupySlot += OnOccupySlot;
        }

        private void OnOccupySlot(Transform slot, Transform item)
        {
            _dirtyCount++;
        }

        private void RefreshState()
        {
            var cargo = _character.Cargo;
            var count = _displayedCount;

            switch (_character.QueueState)
            {
                case NpcQueueState.InQueue:
                    _passiveState.SetActive(true);
                    _activeState.SetActive(false);
                    break;
                
                case NpcQueueState.Buying:
                    _passiveState.SetActive(false);
                    _activeState.SetActive(true);
                
                    _numberWidget.SetNumber((int)((cargo.Slots.Count * _numberMultiplier) - (count * _numberMultiplier)));
                    var percent = (float)count / cargo.Slots.Count;
                    if (_fillRemap.x > 0f || _fillRemap.y < 1f)
                        percent = percent.Remap(0f, 1f, _fillRemap.x, _fillRemap.y);
                    _fillImage.fillAmount = count == 0 ? 0f : percent;
                    break;
                
                case NpcQueueState.Leaving:
                    _passiveState.SetActive(false);
                    _activeState.SetActive(false);
                    break;
            }
        }
    }
}