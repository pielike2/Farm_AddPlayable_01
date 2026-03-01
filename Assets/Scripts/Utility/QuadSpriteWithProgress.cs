using UnityEngine;

namespace Utility
{
    public class QuadSpriteWithProgress : QuadSprite
    {
        [SerializeField] private float _progress = 0.5f;
            
        private static readonly int Hash_Progress = Shader.PropertyToID("_Progress");

        public float Progress
        {
            get => _progress;
            set { _progress = value; Refresh(); }
        }
        
        protected override void SetupPropertyBlock()
        {
            base.SetupPropertyBlock();
            
            _mpb.SetFloat(Hash_Progress, Progress);
        }
    }
}