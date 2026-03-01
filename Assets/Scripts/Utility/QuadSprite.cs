#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utility
{
    public class QuadSprite : MonoBehaviour
    {
        [SerializeField] protected Renderer _renderer;
        [SerializeField] private Texture2D _texture;
        [SerializeField] private Color _tint = Color.white;

        private static readonly int Hash_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Hash_Color = Shader.PropertyToID("_Color");

        protected MaterialPropertyBlock _mpb;
        private bool _isMpbInitialized;

        public Texture2D Texture
        {
            get => _texture;
            set { _texture = value; Refresh(); }
        }

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
            TryInitPropertyBlock();
            Init();
            Refresh();
        }

        protected virtual void Init()
        {
        }

        private void OnValidate()
        {
            _mpb ??= new MaterialPropertyBlock();
            Refresh();
        }

        protected void Refresh()
        {
            TryInitPropertyBlock();
            SetupPropertyBlock();
            _renderer.SetPropertyBlock(_mpb);
        }

        private void TryInitPropertyBlock()
        {
            if (_isMpbInitialized)
                return;
            _isMpbInitialized = true;
            
            _mpb = new MaterialPropertyBlock();
        }

        protected virtual void SetupPropertyBlock()
        {
            _mpb.SetTexture(Hash_MainTex, _texture);
            _mpb.SetColor(Hash_Color, _tint);
        }

#if UNITY_EDITOR
        [ContextMenu("Scale Native Size")]
        public void ScaleNativeSize()
        {
            if (_texture == null)
                return;
            
            var ar = (float)_texture.width / _texture.height;
            var scale = transform.localScale;
            scale.x = scale.y * ar;
            Undo.RecordObject(transform, "Scale Native Size");
            transform.localScale = scale;
        }
#endif
    }
}