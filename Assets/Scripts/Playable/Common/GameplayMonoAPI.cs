using Base;
using UnityEngine;

namespace Playable
{
    public class GameplayMonoAPI : MonoBehaviour
    {
        public void API_ToggleCharacterControl(bool active)
        {
            if (active)
                Get.Input.CharacterControlBlocker.RemoveSource(this);
            else
                Get.Input.CharacterControlBlocker.AddSource(this);
        }
        
        public void API_TogglePlacementPayment(bool active)
        {
            if (active)
                Get.Input.PlacementPaymentBlocker.RemoveSource(this);
            else
                Get.Input.PlacementPaymentBlocker.AddSource(this);
        }
    }
}