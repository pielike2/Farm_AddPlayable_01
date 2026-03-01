using System.Collections.Generic;
using Base.PoolingSystem;
using Playable;
using UnityEngine;
using Utility;

namespace Base
{
    public class AudioController : MonoSingleton<AudioController>
    {
        [SerializeField] private SoundEffect _sourcePrefab;

        private float _playRadiusInnerSqr;
        private float _playRadiusOuterSqr;
        private MarkedReference<Transform> _mainCharacterRef = new MarkedReference<Transform>(PlayableConstants.RefId_MainCharacter);
        private List<SoundEffect> _soundEffects = new List<SoundEffect>(100);
        private List<SecondaryAudioSource> _secondarySources = new List<SecondaryAudioSource>();
        
        public bool IsMuted { get; private set; }

        protected override void Init()
        {
            base.Init();
            
            _playRadiusInnerSqr = Get.Visuals.soundPlayRadiusInner * Get.Visuals.soundPlayRadiusInner;
            _playRadiusOuterSqr = Get.Visuals.soundPlayRadiusOuter * Get.Visuals.soundPlayRadiusOuter;
        }

        public void AddSecondarySource(SecondaryAudioSource source)
        {
            _secondarySources.Add(source);
        }

        public SoundEffect PlayClip(AudioClip clip, Vector3 position, bool loop, float volume = 1f, float pitch = 1f)
        {
            var e = _sourcePrefab.Spawn(position);
            e.Source.loop = loop;
            e.Source.volume = volume;
            e.Source.pitch = pitch;
            e.Source.mute = IsMuted;
            e.Play(clip);
            _soundEffects.Add(e);
            return e;
        }

        public SoundEffect PlayClip(AudioClip clip, bool loop, float volume = 1f, float pitch = 1f)
        {
            return PlayClip(clip, Vector3.zero, loop, volume, pitch);
        }

        public SoundEffect PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return PlayClip(clip, Vector3.zero, false, volume, pitch);
        }

        public bool TryPlayByDistance(AudioClip clip, Vector3 position, bool loop, float volume, float pitch, 
            out SoundEffect e, Transform parent = null)
        {
            var sqrDist = Vector3.SqrMagnitude(position - _mainCharacterRef.Value.position);
            
            if (sqrDist > _playRadiusOuterSqr)
            {
                e = null;
                return false;
            }
            
            e = _sourcePrefab.Spawn(position);
            
            if (loop && parent != null)
                e.transform.parent = parent;
            
            e.Source.loop = loop;
            e.Source.volume = volume * Mathf.InverseLerp(sqrDist, _playRadiusOuterSqr, _playRadiusInnerSqr);
            e.Source.pitch = pitch;
            e.Source.mute = IsMuted;
            
            if(loop)
                e.PlayLoopWithDistance(clip, _mainCharacterRef.Value, _playRadiusOuterSqr, _playRadiusInnerSqr, volume);
            else e.Play(clip);
            
            _soundEffects.Add(e);
            return true;
        }

        public void ToggleMute(bool mute)
        {
            if (IsMuted == mute)
                return;
            IsMuted = mute;
            
            for (int i = 0; i < _soundEffects.Count; i++)
                _soundEffects[i].Source.mute = mute;
            for (int i = 0; i < _secondarySources.Count; i++)
                _secondarySources[i].SetMute(mute);
        }
    }
}