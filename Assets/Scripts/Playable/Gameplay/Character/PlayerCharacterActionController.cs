using UnityEngine;

namespace Playable.Gameplay.Character
{
    public class PlayerCharacterActionController
    {
        private readonly IPlayerCharacter _character;
        private readonly Animator _animator;
        
        public int LastActionId { get; private set; }

        public PlayerCharacterActionController(IPlayerCharacter character, Animator animator)
        {
            _character = character;
            _animator = animator;
        }

        public void StartAction(int actionTypeId, bool asBooleanAction = true, params object[] args)
        {
            LastActionId = actionTypeId;
            _animator.SetInteger(PlayableConstants.AnimHash_ActionType, actionTypeId);
            if (asBooleanAction)
                _animator.SetBool(PlayableConstants.AnimHash_IsDoingAction, true);
            _animator.SetTrigger(PlayableConstants.AnimHash_StartAction);
        }

        public void StopAction(int actionTypeId)
        {
            if (LastActionId == actionTypeId)
                StopActions();
        }

        public void StopActions()
        {
            LastActionId = -1;
            _animator.SetInteger(PlayableConstants.AnimHash_ActionType, -1);
            _animator.SetBool(PlayableConstants.AnimHash_IsDoingAction, false);
        }
    }
}