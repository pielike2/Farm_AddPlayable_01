using System;
using UnityEngine;

namespace Base
{
    [Serializable]
    public class AudioClipShell
    {
        public AudioClip clip;
        [Range(0.01f, 1f)] public float volume = 0.5f;
        public float pitch = 1f;

        private bool _isInitialized;
        private bool _isValid;

        public bool IsValid => _isValid;

        private void TryInitialize()
        {
            if (_isInitialized)
                return;
            _isValid = clip != null;
            _isInitialized = true;
        }

        public SoundEffect Play(float volumeMult = 1f, float pitchMult = 1f)
        {
            TryInitialize();
            return IsValid ? AudioController.Instance.PlayClip(clip, volume * volumeMult, pitch * pitchMult) : null;
        }

        public SoundEffect Playloop(float volumeMult = 1f, float pitchMult = 1f)
        {
            TryInitialize();
            return IsValid ? AudioController.Instance.PlayClip(clip, true, volume * volumeMult, pitch * pitchMult) : null;
        }
        
        public bool TryPlayByDistanceLoop(Vector3 position, Transform parent, out SoundEffect e, float volumeMult = 1f, float pitchMult = 1f)
        {
            TryInitialize();
            e = null;
            return IsValid && AudioController.Instance.TryPlayByDistance(clip, position, true, volume * volumeMult, pitch * pitchMult, out e, parent);
        }

        public bool TryPlayByDistance(Vector3 position, float volumeMult, float pitchMult, out SoundEffect e)
        {
            TryInitialize();
            e = null;
            return IsValid && AudioController.Instance.TryPlayByDistance(clip, position, false, volume * volumeMult, pitch * pitchMult, out e);
        }

        public void TryPlayByDistance(Vector3 position, float volumeMult = 1f, float pitchMult = 1f)
        {
            TryInitialize();
            if (IsValid)
                AudioController.Instance.TryPlayByDistance(clip, position, false, volume * volumeMult, pitch * pitchMult, out _);
        }
    }
}