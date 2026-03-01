using UnityEngine;

namespace Utility
{
    public class SpriteNumberWidget : BaseSpriteNumberWidget
    {
        [SerializeField] private SpriteRenderer[] _sprites = new SpriteRenderer[0];

        protected override int RenderersCount => _sprites.Length;
        
        protected override void SetRendererActive(int i, bool active)
        {
            _sprites[i].gameObject.SetActive(active);
        }

        protected override void SetRendererSprite(int i, Sprite sprite)
        {
            _sprites[i].sprite = sprite;
        }
    }
}