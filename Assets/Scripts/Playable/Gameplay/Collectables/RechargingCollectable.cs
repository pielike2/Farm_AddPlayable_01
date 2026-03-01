using System.Collections;
using Base;
using Base.PoolingSystem;
using Base.SignalSystem;
using Playable.Animations;
using Playable.Signals;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Playable.Gameplay.Collectables
{
    public class RechargingCollectable : BaseCollectable
    {
        [SerializeField] private float _baseHealth = 3;
        [SerializeField] private Collider _collider;
        [SerializeField] private float _rechargeDelay = 3f;
        [SerializeField] private float _deactivateDelay = 0.2f;

        [SerializeField] private bool _toggleStatesOnDamage;
        [SerializeField] private Transform[] _rechargedTransforms;
        [SerializeField] private BaseSimpleTweenAnimation _rechargeAppearAnim;
        [SerializeField] private float _rechargeAppearAnimInterval = 0.1f;
        [SerializeField] private bool _rechargeOnStart;
        
        [SerializeField] private ScriptableSignal _signalOnDeath;

        [SerializeField] private OneShotParticleEffect _hitFx;
        
        [SerializeField] private UnityEvent _onDie;
        
        private float _currentHealth;
        private bool _isDead;
        private BaseCollectableLootSpawner _lootSpawner;
        private bool _lootSpawnActive;
        private bool _validHitFx;
        private Coroutine _rechargeCoroutine;
        private int _activePartsCount;
        private float _healthPerPart;

        public override bool IsAlive => !_isDead;
        public override bool IsColliderActive => _collider.enabled;

        private void Awake()
        {
            _currentHealth = _baseHealth;

            _lootSpawner = GetComponent<BaseCollectableLootSpawner>();
            _lootSpawnActive = _lootSpawner != null;
            
            _activePartsCount = _rechargedTransforms.Length;
            _healthPerPart = (float)_baseHealth / _rechargedTransforms.Length;

            _validHitFx = _hitFx != null;
            if (_validHitFx)
            {
                _hitFx.IsReleaseDisabled = true;
                _hitFx.gameObject.SetActive(false);
            }

            Get.SignalBus.Subscribe<SDelayRechargingCollectables>(e =>
            {
                if (_isDead && _rechargeCoroutine != null)
                {
                    StopCoroutine(_rechargeCoroutine);
                    _rechargeCoroutine = StartCoroutine(RechargeRoutine());
                }
            });
            
            if (_rechargeOnStart)
                Recharge();
        }

        public override void TakeDamage(float damage, GameObject source)
        {
            base.TakeDamage(damage, source);
            
            if (_isDead)
                return;
            
            _currentHealth -= damage;
            if (_currentHealth < 0f)
                _currentHealth = 0f;

            if (_validHitFx)
                _hitFx.Spawn(transform.position, transform.rotation);
            
            if (_toggleStatesOnDamage)
            {
                var targetActiveParts = Mathf.CeilToInt(_currentHealth / _healthPerPart);
                while (_activePartsCount > targetActiveParts && _activePartsCount > 0)
                {
                    _activePartsCount--;
                    _rechargedTransforms[_activePartsCount].gameObject.SetActive(false);
                }
            }
            
            if (_currentHealth < Mathf.Epsilon)
                Die(source);
        }
        
        public void ForceRecharge()
        {
            if(_rechargeCoroutine != null)
                StopCoroutine(_rechargeCoroutine);
            
            Recharge();
        }

        private void Die(GameObject damageSource)
        {
            if (_isDead)
                return;
            _isDead = true;
            
            if (_lootSpawnActive)
                _lootSpawner.SpawnLoot(damageSource);
            
            if (_signalOnDeath != null)
                _signalOnDeath.Trigger(this);
            
            _onDie?.Invoke();
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(_deactivateDelay);

            _collider.enabled = false;

            if (_rechargeCoroutine == null && _rechargeDelay > 0)
                _rechargeCoroutine = StartCoroutine(RechargeRoutine());
        }

        private IEnumerator RechargeRoutine()
        {
            for (int i = 0; i < _rechargedTransforms.Length; i++)
                _rechargedTransforms[i].gameObject.SetActive(false);
            
            yield return new WaitForSeconds(_rechargeDelay);
            
            _rechargeCoroutine = null;
            _activePartsCount = _rechargedTransforms.Length;
            
            if (_isDead)
                Recharge();
        }

        private void Recharge()
        {
            _currentHealth = _baseHealth;
            _isDead = false;

            _collider.enabled = true;

            for (var i = 0; i < _rechargedTransforms.Length; i++)
            {
                _rechargedTransforms[i].gameObject.SetActive(true);
                _rechargeAppearAnim.Animate(_rechargedTransforms[i], _rechargeAppearAnimInterval * i);
            }
        }
    }
}