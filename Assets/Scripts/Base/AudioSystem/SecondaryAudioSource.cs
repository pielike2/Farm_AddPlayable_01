using System;
using UnityEngine;

namespace Base
{
    [RequireComponent(typeof(AudioSource))]
    public class SecondaryAudioSource : MonoBehaviour
    {
        [SerializeField, HideInInspector] private AudioSource _source;
        [SerializeField] private float _defaultVolume = 0.5f;

        private void Reset()
        {
            _source = GetComponent<AudioSource>();
        }

        private void OnValidate()
        {
            _defaultVolume = _source.volume;
        }

        private void Start()
        {
            AudioController.Instance.AddSecondarySource(this);
        }

        public void SetMute(bool mute)
        {
            _source.mute = mute;
            if (!mute)
                _source.volume = _defaultVolume;
        }
    }
}