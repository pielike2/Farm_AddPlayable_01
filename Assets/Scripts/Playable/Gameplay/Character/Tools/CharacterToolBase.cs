using UnityEngine;

namespace Playable.Gameplay.Character
{
    public interface ICharacterTool
    {
        Transform CharacterRoot { get; }
        int CharacterCustomStateId { get; }
        void ToggleTool(bool active);
    }
    
    public class CharacterToolBase : MonoBehaviour, ICharacterTool
    {
        [SerializeField] protected Animator _animator;
        [SerializeField] private int _characterCustomStateId = 10;
        [SerializeField] private Transform _characterRoot;
        [SerializeField] private GameObject[] _toolObjects = new GameObject[0];
        [SerializeField] private bool _useCustomCharacterSkin;
        [SerializeField] private int _customCharacterSkinIndex = 0;

        [SerializeField] private float _speedAnimation = 1f;
        [SerializeField] private float _speedMovement = 1f;
        
        public bool IsToolActive { get; private set; }
        public Transform CharacterRoot => _characterRoot;

        public virtual int CharacterCustomStateId => _characterCustomStateId;
        public float SpeedAnimation => _speedAnimation;
        public float SpeedMovement => _speedMovement;
        public bool UseCustomCharacterSkin => _useCustomCharacterSkin;
        public int CustomCharacterSkinIndex => _customCharacterSkinIndex;

        public virtual void ToggleTool(bool active)
        {
            if (IsToolActive == active)
                return;
            IsToolActive = active;
            
            _animator.SetBool(PlayableConstants.AnimHash_IsDoingAction, false);
            
            ToggleToolObjects(active);
            OnToggleTool();
        }

        public void ToggleToolObjects(bool active)
        {
            for (int i = 0; i < _toolObjects.Length; i++)
                _toolObjects[i].SetActive(active);
        }

        protected virtual void OnToggleTool()
        {
        }
    }
}