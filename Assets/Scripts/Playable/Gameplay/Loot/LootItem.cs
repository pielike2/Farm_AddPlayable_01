using System;
using System.Collections;
using Base.PoolingSystem;
using DG.Tweening;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Loot
{
    public class LootItem : MonoBehaviour, ILootItem, IMonoPoolObject
    {
        [SerializeField] private HashId _itemTypeId;
        [SerializeField] private bool _usesRigidbody;
        [SerializeField] private bool _isCurrencyItem;

        public HashId ItemTypeId => _itemTypeId;
        public ILootItem NextSourcePrefab { get; set; }
        public bool WasPickedUpOnce { get; private set; }
        public bool IsUsable { get; private set; }
        public bool HasRigidbody { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public bool IsCurrencyItem => _isCurrencyItem;

        private Coroutine _disappearCoroutine;

        private void Awake()
        {
            if (_usesRigidbody)
            {
                Rigidbody = GetComponent<Rigidbody>();
                HasRigidbody = Rigidbody != null;
            }
        }

        public void OnSpawnFromPool()
        {
            IsUsable = true;
        }

        public void OnReturnToPool()
        {
            NextSourcePrefab = null;
            WasPickedUpOnce = false;
            IsUsable = true;
            _disappearCoroutine = null;
            ToggleRigidbody(false);
        }

        public bool TryPickup()
        {
            if (!IsUsable)
                return false;
            
            WasPickedUpOnce = true;
            
            transform.DOKill();
            var rb = GetComponent<Rigidbody>();
            if (rb)
                rb.isKinematic = true;

            return true;
        }

        public void DisappearAfterDelay(float delay, float endScale, float disappearDuration)
        {
            if (_disappearCoroutine != null)
                return;
            StartCoroutine(DisappearAfterDelayRoutine(delay, endScale, disappearDuration));
        }

        private IEnumerator DisappearAfterDelayRoutine(float delay, float endScale, float disappearDuration)
        {
            yield return new WaitForSeconds(delay);

            if (WasPickedUpOnce)
                yield break;

            IsUsable = false;
            transform.DOScale(endScale, disappearDuration).SetEase(Ease.InCubic);
            yield return new WaitForSeconds(disappearDuration);
            this.Release();
        }
        
        public void ToggleRigidbody(bool value)
        {
            if (HasRigidbody)
                Rigidbody.isKinematic = !value;
        }
        
        public void CopyFromSourcePrefab(ILootItem other)
        {
            _itemTypeId = other.ItemTypeId;
            _isCurrencyItem = other.IsCurrencyItem;
            NextSourcePrefab = other;
        }
    }
}