using UnityEngine;

namespace Playable
{
    public static class PlayableConstants
    {
        public const string RefId_MainCharacter = "MainCharacter";
        
        public static readonly int AnimHash_IsCustomStateActive = Animator.StringToHash("IsCustomStateActive");
        public static readonly int AnimHash_CustomStateType = Animator.StringToHash("CustomStateType");
        public static readonly int AnimHash_UpdateCustomState = Animator.StringToHash("UpdateCustomState");
        
        public static readonly int AnimHash_StartAction = Animator.StringToHash("StartAction");
        public static readonly int AnimHash_IsDoingAction = Animator.StringToHash("IsDoingAction");
        public static readonly int AnimHash_ActionType = Animator.StringToHash("ActionType");
        
        public const string ComponentMenu_Root = "-[MC] ";
        public const string ComponentMenu_Utility = ComponentMenu_Root + "Utility/";
        public const string ComponentMenu_Player = ComponentMenu_Root + "Player/";
        public const string ComponentMenu_Gameplay = ComponentMenu_Root + "Gameplay/";
        public const string ComponentMenu_Production = ComponentMenu_Root + "Gameplay/Production/";
        public const string ComponentMenu_Npc = ComponentMenu_Root + "NPC";
    }
}