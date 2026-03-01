using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Utility
{
    [ExecuteAlways]
    public class ImageNumberWidget : BaseSpriteNumberWidget
    {
        [SerializeField] private Image[] _images = new Image[0];
        [SerializeField] private bool _preserveAR;
        [SerializeField, Range(0.01f, 1f)] private float _preservedARWeight = 1f;
        [SerializeField] private bool _useCentering = true;
        [SerializeField] private float _spacing = 1f;

        protected override int RenderersCount => _images.Length;
        
        protected override void SetRendererActive(int i, bool active)
        {
            _images[i].gameObject.SetActive(active);
        }

        protected override void SetRendererSprite(int i, Sprite sprite)
        {
            _images[i].sprite = sprite;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_images.All(item => item != null))
                PostprocessNumber(_images.Count(item => item.gameObject.activeSelf));
        }
#endif

        protected override void PostprocessNumber(int activeDigits)
        {
            if (_preserveAR) 
                PreserveAR();
            
            UpdatePositions(activeDigits);
        }

        private void UpdatePositions(int activeDigits)
        {
            if (activeDigits == 0)
                return;
            
            var centerOffset = 0f;
            
            if (_useCentering)
                for (int i = 1; i < activeDigits; i++)
                    centerOffset += _images[i].rectTransform.sizeDelta.x * 0.5f;

            var offset = 0f;
            for (int i = 0; i < activeDigits; i++)
            {
                var pos = Vector3.right * (offset - centerOffset);
                _images[i].rectTransform.localPosition = pos;
                var sizeOffset = _images[i].rectTransform.sizeDelta.x * 0.5f;
                if (i + 1 < activeDigits)
                    sizeOffset += _images[i + 1].rectTransform.sizeDelta.x * 0.5f;
                offset += sizeOffset + _spacing;
            }
        }

        private void PreserveAR()
        {
            foreach (var image in _images)
            {
                if (!image.gameObject.activeSelf)
                    continue;
                var t = image.rectTransform;
                var basicSize = t.sizeDelta.y;

                var tex = image.sprite.texture;
                var ar = (float)tex.width / tex.height;
                var weightedAR = _preservedARWeight < 0.999f
                    ? Mathf.Lerp(1f, ar, _preservedARWeight)
                    : ar;
                
                var size = new Vector2(weightedAR * basicSize, basicSize);
                t.sizeDelta = size;
            }
        }
    }
}