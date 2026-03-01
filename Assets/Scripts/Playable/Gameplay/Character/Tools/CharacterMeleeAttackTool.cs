using System.Linq;
using Base;
using Playable.Gameplay.Loot;
using UnityEngine;
using Utility;
using Utility.Extensions;
using Utility.SensorSystem;

namespace Playable.Gameplay.Character
{
    public class CharacterMeleeAttackTool : CharacterToolBase
    {
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] protected MecanimEventReceiver _animEventReceiver;
        [SerializeField] private Transform _attackCenter;
        [SerializeField] private float _attackRadius = 0.5f;
        [SerializeField] private HashId _attackTargetId;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _attackStopDelay = 0.1f;
        [SerializeField] private float _attackDamage = 1f;
        [SerializeField] private AudioClipShell _attackSfx;
        [SerializeField] private ParticleSystem _attackFx;

        private bool _isAttacking;
        private bool _isStoppingAttack;
        private float _attackCooldownTime;
        private float _attackStopTime;
        private SensorFilter _sensorFilter;

        private void Awake()
        {
            _sensorFilter = _sensor.GetFilter(_attackTargetId);
        }

        private void Update()
        {
            var needAttack = _sensorFilter.FilteredTargets.Any();

            if (needAttack)
                _isAttacking = true;
            
            if (_isAttacking && !needAttack && !_isStoppingAttack)
            {
                _attackStopTime = Time.time + _attackStopDelay;
                _isStoppingAttack = true;
            }
            if (_isStoppingAttack && Time.time > _attackStopTime)
            {
                _isAttacking = false;
                _isStoppingAttack = false;
            }
            
            _animator.SetBool(PlayableConstants.AnimHash_IsDoingAction, _isAttacking);
        }

        public override void ToggleTool(bool active)
        {
            base.ToggleTool(active);

            if (active)
            {
                _animEventReceiver.OnReceiveInt += TryApplyAttack;
            }
            else
            {
                _animEventReceiver.OnReceiveInt -= TryApplyAttack;
            }
        }

        private void TryApplyAttack(int i)
        {
            if (i == 0)
                ApplyAttack();
        }

        private void OnDrawGizmos()
        {
            if (_attackCenter)
            {
                Gizmos.color = Color.yellow.SetAlpha(0.3f);
                Gizmos.DrawWireSphere(_attackCenter.position, _attackRadius);
            }   
        }

        public void ApplyAttack()
        {
            if (Time.time < _attackCooldownTime)
                return;
            _attackCooldownTime = Time.time + _attackCooldown;
            
            if(_attackFx != null)
                _attackFx.Play();
            
            _attackSfx.Play(1f, Random.Range(0.95f, 1.1f));
            
            var hitsCount = Physics.OverlapSphereNonAlloc(_attackCenter.position, _attackRadius, Get.CollidersBuffer);
            for (int i = 0; i < hitsCount; i++)
            {
                var hit = Get.CollidersBuffer[i];
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && damageable.SenseId == _attackTargetId)
                    damageable.TakeDamage(_attackDamage, gameObject);
            }
        }
    }
}