using System;
using UnityEngine;
using Utility.SensorSystem;
using Object = UnityEngine.Object;

namespace Playable.Gameplay
{
    public interface IDamageable : ISensorTarget
    {
        bool IsAlive { get; }
        event Action<float, Object> OnTakeDamage; 
        void TakeDamage(float damage, GameObject source);
    }
}