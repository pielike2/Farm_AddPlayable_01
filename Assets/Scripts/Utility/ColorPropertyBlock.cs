using DG.Tweening;
using UnityEngine;

namespace Utility
{
    public class ColorPropertyBlock : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private string _propertyName = "_BaseColor";
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Color _tint = Color.white;

        private int _colorHash;
        private MaterialPropertyBlock _mpb;
        private bool _toggleActive;
        
        public Color Tint
        {
            get => _tint;
            set { _tint = value; Refresh(); }
        }

        public float Alpha
        {
            get => _tint.a;
            set { _tint.a = value; Refresh(); }
        }

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _colorHash = Shader.PropertyToID(_propertyName);
            Refresh();
        }

        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void OnValidate()
        {
            _mpb ??= new MaterialPropertyBlock();
            _colorHash = Shader.PropertyToID(_propertyName);
            Refresh();
        }

        private void Refresh()
        {
            SetupPropertyBlock();
            _renderer.SetPropertyBlock(_mpb);
        }

        private void SetupPropertyBlock()
        {
            _mpb.SetColor(_colorHash, _color * _tint);
        }

        public Tween FadeIn(float duration)
        {
            return DOVirtual.Float(Alpha, 1f, duration, value => Alpha = value).SetEase(Ease.OutCubic).SetTarget(_renderer);
        }
        
        public Tween FadeOut(float duration)
        {
            return DOVirtual.Float(Alpha, 0f, duration, value => Alpha = value).SetEase(Ease.InCubic).SetTarget(_renderer);
        }

        public void Toggle(bool active)
        {
            _renderer.DOKill();
            _toggleActive = active;
            gameObject.SetActive(active);
        }

        public void ToggleFaded(bool active, float fadeInDuration, float fadeOutDuration)
        {
            if (_toggleActive == active)
                return;
            _toggleActive = active;
            
            _renderer.DOKill();
            if (_toggleActive)
            {
                gameObject.SetActive(true);
                FadeIn(fadeInDuration);
            }
            else
            {
                var tween = FadeOut(fadeOutDuration);   
                tween.onComplete = () => gameObject.SetActive(false);
            }
        }
    }
}