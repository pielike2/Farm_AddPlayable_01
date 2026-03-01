using System;
using UnityEngine;

namespace Utility
{
    public class ComplexSkinSwitcher : MonoBehaviour
    {
        [Serializable]
        public class SkinConfig
        {
            public Renderer[] renderers = {};
            public GameObject[] gameObjects = {};
        }

        [SerializeField] private SkinConfig[] _skins;
        
        private int _currentSkinIndex = -1;
        
        public void SetSkin(int index)
        {
            if (index < 0 || index >= _skins.Length || _currentSkinIndex == index)
                return;

            _currentSkinIndex = index;

            for (var i = 0; i < _skins[index].renderers.Length; i++)
            {
                _skins[index].renderers[i].enabled = true;
                _skins[index].renderers[i].gameObject.SetActive(true);
            }
            for (var i = 0; i < _skins[index].gameObjects.Length; i++)
            {
                _skins[index].gameObjects[i].SetActive(true);
            }
            
            for (int i = 0; i < _skins.Length; i++)
            {
                if (i == index)
                    continue;
                
                for (var j = 0; j < _skins[i].renderers.Length; j++)
                {
                    _skins[i].renderers[j].enabled = false;
                    _skins[i].renderers[j].gameObject.SetActive(false);
                }
                for (var j = 0; j < _skins[i].gameObjects.Length; j++)
                {
                    _skins[i].gameObjects[j].SetActive(false);
                }
            }
        }
    }
}