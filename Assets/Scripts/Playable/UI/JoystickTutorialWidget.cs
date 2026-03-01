using Base;
using Playable.Gameplay.Character;
using UnityEngine;
using Utility;

namespace Playable.UI
{
    public class JoystickTutorialWidget : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private float _activationDelay = 3f;

        private float _deactivationTime;
        private MarkedReference<CharacterMovement> _characterMovement = new MarkedReference<CharacterMovement>(PlayableConstants.RefId_MainCharacter);

        private void Update()
        {
            if (_characterMovement.IsNull)
            {
                ForceDeactivate();
                return;
            }
            
            if (_characterMovement.Value.IsMoving || Get.Input.CharacterControlBlocker.IsActive)
            {
                ForceDeactivate();
                return;
            }
            
            if (Time.time > _deactivationTime + _activationDelay && !_root.activeSelf)
                _root.SetActive(true);
        }

        public void ForceDeactivate()
        {
            if (!_root.activeSelf)
                return;
            _deactivationTime = Time.time;
            _root.SetActive(false);
        }
    }
}