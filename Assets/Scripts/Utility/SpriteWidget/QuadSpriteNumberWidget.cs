using System.Linq;
using UnityEngine;

namespace Utility
{
    [ExecuteAlways]
    public class QuadSpriteNumberWidget : BaseSpriteNumberWidget
    {
        [SerializeField] private QuadSprite[] _sprites = new QuadSprite[0];
        [SerializeField] private bool _preserveAR;
        [SerializeField, Range(0.01f, 1f)] private float _preservedARWeight = 1f;
        [SerializeField] private bool _useCentering = true;
        [SerializeField] private float _spacing = 0.1f;

        protected override int RenderersCount => _sprites.Length;
        
        protected override void SetRendererActive(int i, bool active)
        {
            _sprites[i].gameObject.SetActive(active);
        }

        protected override void SetRendererSprite(int i, Sprite sprite)
        {
            _sprites[i].Texture = sprite.texture;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_sprites.All(item => item != null))
                PostprocessNumber(_sprites.Count(item => item.gameObject.activeSelf));
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
                    centerOffset += _sprites[i].transform.localScale.x * 0.5f;

            var offset = 0f;
            for (int i = 0; i < activeDigits; i++)
            {
                var pos = Vector3.right * (offset - centerOffset);
                _sprites[i].transform.localPosition = pos;
                var sizeOffset = _sprites[i].transform.localScale.x * 0.5f;
                if (i + 1 < activeDigits)
                    sizeOffset += _sprites[i + 1].transform.localScale.x * 0.5f;
                offset += sizeOffset + _spacing;
            }
        }

        private void PreserveAR()
        {
            foreach (var sprite in _sprites)
            {
                if (!sprite.gameObject.activeSelf)
                    continue;
                var t = sprite.transform;
                var basicScale = t.localScale.y;
                
                var ar = (float)sprite.Texture.width / sprite.Texture.height;
                var weightedAR = _preservedARWeight < 0.999f
                    ? Mathf.Lerp(1f, ar, _preservedARWeight)
                    : ar;
                
                var scale = new Vector3(weightedAR * basicScale, basicScale, 1f);
                t.localScale = scale;
            }
        }
    }
}