using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Character
{
    public class CharacterStateController : MonoBehaviour
    {
        [SerializeField] private int _defaultStateIndex = -1;
        [SerializeField] private List<CharacterToolBase> _tools = new List<CharacterToolBase>();
        [SerializeField] private Animator _animator;
        [SerializeField] private ComplexSkinSwitcher _skinSwitcher;
        
        [Header("Debug")]
        [SerializeField] private bool _debugActive;

        private bool _skinSwitcherValid;

        public int ActiveStateIndex { get; private set; } = -1;

        private void Awake()
        {
            _skinSwitcherValid = _skinSwitcher != null;
            
            for (int i = 0; i < _tools.Count; i++)
            {
                if (_tools[i] == null) continue;
                _tools[i].ToggleToolObjects(false);
                TryToggleToolObject(_tools[i], false);
            }
            
            if (_skinSwitcherValid)
                _skinSwitcher.SetSkin(0);
        }

        private void Start()
        {
            if (_defaultStateIndex >= 0)
                ActivateStateByIndex(_defaultStateIndex);
            else
                DeactivateCustomState();
        }

        private void Update()
        {
            if (_debugActive && Input.GetKeyDown(KeyCode.T))
            {
                if (ActiveStateIndex == _tools.Count - 1)
                    DeactivateCustomState();
                else
                    ActivateStateByIndex(ActiveStateIndex + 1);
            }
        }

        public void ActivateStateByTypeId(int stateTypeId)
        {
            if (stateTypeId < 0)
            {
                DeactivateCustomState();
                return;
            }
            
            for (int i = 0; i < _tools.Count; i++)
            {
                var tool = _tools[i];
                if (tool == null || tool.CharacterCustomStateId != stateTypeId)
                    continue;
                ActivateStateByIndex(i);
                return;
            }
        }

        public bool ActivateStateByIndex(int index)
        {
            if (index < 0 || index >= _tools.Count)
                return false;

            var tool = _tools[index];
            if (tool == null)
                return false;

            if (ActiveStateIndex >= 0 && ActiveStateIndex < _tools.Count)
            {
                var prevTool = _tools[ActiveStateIndex];
                if (prevTool != null)
                {
                    prevTool.ToggleTool(false);
                    TryToggleToolObject(prevTool, false);
                }
            }

            ActiveStateIndex = index;

            tool.ToggleTool(true);
            TryToggleToolObject(tool, true);
            
            _animator.SetInteger(PlayableConstants.AnimHash_CustomStateType, tool.CharacterCustomStateId);
            _animator.SetBool(PlayableConstants.AnimHash_IsCustomStateActive, tool.CharacterCustomStateId >= 0);
            _animator.SetTrigger(PlayableConstants.AnimHash_UpdateCustomState);
            
            if (tool.UseCustomCharacterSkin && _skinSwitcherValid)
                _skinSwitcher.SetSkin(tool.CustomCharacterSkinIndex);

            return true;
        }
        
        private void TryToggleToolObject(CharacterToolBase tool, bool active)
        {
            if (tool != null && tool.transform != transform)
                tool.gameObject.SetActive(active);
        }

        public void DeactivateCustomState()
        {
            if (ActiveStateIndex >= 0 && ActiveStateIndex < _tools.Count)
            {
                var tool = _tools[ActiveStateIndex];
                if (tool != null)
                {
                    tool.ToggleTool(false);
                    TryToggleToolObject(tool, false);
                }
            }
            
            ActiveStateIndex = -1;
            _animator.SetInteger(PlayableConstants.AnimHash_CustomStateType, 0);
            _animator.SetBool(PlayableConstants.AnimHash_IsCustomStateActive, false);
            _animator.SetTrigger(PlayableConstants.AnimHash_UpdateCustomState);
        }
    }
}