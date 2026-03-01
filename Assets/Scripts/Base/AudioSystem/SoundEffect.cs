using Base.PoolingSystem;
using UnityEngine;
using UnityEngine.Audio;

namespace Base
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffect : MonoBehaviour, IMonoPoolObject
    {
        [SerializeField] private AudioSource _source;

        private float _releaseTime = -1;
        private float _volume = 1f;
        private float _radiusOuterSqr;
        private float _radiusInnerSqr;
        private Transform _target;
        private float _sqrDist;
        
        public AudioSource Source => _source;

        public void OnSpawnFromPool()
        {
        }

        public void OnReturnToPool()
        {
            _releaseTime = -1;
            _source.Stop();
            Source.loop = false;
            Source.volume = 1f;
            Source.pitch = 1f;
        }

        private void Update()
        {
            if (_releaseTime > 0f && Time.time > _releaseTime)
                this.Release();

            if (_source.loop && _target != null)
            {
                _sqrDist = Vector3.SqrMagnitude(transform.position - _target.position);
                _source.volume = _volume * Mathf.InverseLerp(_sqrDist, _radiusOuterSqr, _radiusInnerSqr);
            }
        }

        public void Play(AudioClip clip, AudioMixerGroup outputGroup = null)
        {
            _source.outputAudioMixerGroup = outputGroup;
            _source.clip = clip;
            _source.time = 0;
            _source.Play();

            if (_source.loop)
                _releaseTime = -1;
            else
                ReleaseAfter(_source.clip.length);
        }
        
        public void PlayLoopWithDistance(AudioClip clip, Transform target, float radiusOuterSqr, float radiusInnerSqr, 
            float volume, AudioMixerGroup outputGroup = null)
        {
            _radiusOuterSqr = radiusOuterSqr;
            _radiusInnerSqr = radiusInnerSqr;
            _target = target;
            _volume = volume;
            
            _source.outputAudioMixerGroup = outputGroup;
            _source.clip = clip;
            _source.time = 0;
            _source.Play();

            if (_source.loop)
                _releaseTime = -1;
            else
                ReleaseAfter(_source.clip.length);
        }

        public void ReleaseAfter(float delay)
        {
            _releaseTime = Time.time + delay;
        }
    }
}