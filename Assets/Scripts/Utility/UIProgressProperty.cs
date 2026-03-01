using System;
using UnityEngine;
using UnityEngine.UI;

namespace Utility
{
    [ExecuteAlways]
    public class UIProgressProperty : MonoBehaviour
    {
        [SerializeField] private bool _updateInEditMode;
        [SerializeField] protected Image _renderer;
        [SerializeField] private float _progress = 1f;
            
        private static readonly int Hash_Progress = Shader.PropertyToID("_Progress");

        public float Progress
        {
            get => _progress;
            set { _progress = value; Refresh(); }
        }

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            
#if UNITY_EDITOR
            if (!Application.isPlaying && !_updateInEditMode)
                return;
#endif
            Refresh();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !_updateInEditMode)
                return;
#endif
            Refresh();
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !_updateInEditMode)
                return;
#endif
            Refresh();
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void Refresh()
        {
            _renderer.material.SetFloat(Hash_Progress, Progress);
        }
    }
}