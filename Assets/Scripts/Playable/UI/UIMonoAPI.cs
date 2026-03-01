using Base;
using UnityEngine;

namespace Playable.UI
{
    public class UIMonoAPI : MonoBehaviour
    {
        public void API_ClickCTA()
        {
            Application.OpenURL("https://store-link.com");
        }

        public void API_ToggleMute(bool mute)
        {
            AudioController.Instance.ToggleMute(mute);
        }
    }
}