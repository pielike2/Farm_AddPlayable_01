using System;
using UnityEngine;
using Utility;
using Object = UnityEngine.Object;

namespace Playable.Gameplay.Collectables
{
    public class BaseCollectable : MonoBehaviour, ICollectable
    {
        [SerializeField] private HashId _senseId;

        public HashId SenseId => _senseId;
        public virtual bool IsAlive => true;
        public event Action<float, Object> OnTakeDamage;
        public virtual bool IsColliderActive => true;

        public virtual void TakeDamage(float damage, GameObject source)
        {
            if (!IsAlive)
                return;
            
            OnTakeDamage?.Invoke(damage, source);
        }
    }
}