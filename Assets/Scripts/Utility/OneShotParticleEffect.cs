using System.Collections;
using Base.PoolingSystem;
using UnityEngine;

namespace Utility
{
    public class OneShotParticleEffect : MonoBehaviour, IMonoPoolObject
    {
        [SerializeField] private ParticleSystem _mainParticleSystem;

        private WaitForSeconds _despawnWait;
        private Coroutine _despawnCoroutine;
        
        public bool IsReleaseDisabled { get; set; }

        private void Reset()
        {
            _mainParticleSystem = GetComponentInChildren<ParticleSystem>();
        }

        private void Awake()
        {
            _despawnWait = new WaitForSeconds(_mainParticleSystem.main.duration);
        }

        public void OnSpawnFromPool()
        {
            _mainParticleSystem.Play();
            StartDespawn();
        }

        public void OnReturnToPool()
        {
        }

        public void Play()
        {
            gameObject.SetActive(true);
            _mainParticleSystem.Play();
            StartDespawn();
        }

        private void StartDespawn()
        {
            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnRoutine());
        }

        IEnumerator DespawnRoutine()
        {
            yield return _despawnWait;
            if (IsReleaseDisabled)
                gameObject.SetActive(false);
            else
                this.Release();
        }
    }
}