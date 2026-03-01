using UnityEngine;

namespace Utility
{
    public class SimpleSkinSwitcher : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;

        public int SkinsCount => _renderers.Length;

        public void SetSkin(int index)
        {
            if (index < 0 || index >= _renderers.Length)
                return;

            _renderers[index].gameObject.SetActive(true);
            _renderers[index].enabled = true;
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (i != index)
                {
                    _renderers[i].gameObject.SetActive(false);
                    _renderers[i].enabled = false;
                }
            }
        }
    }
}