using DG.Tweening;
using Playable.Gameplay.Collectables;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Animations
{
    public class BaseLootCollectAnimation : ScriptableObject
    {
        public float[] customCallbackTimes;

        public virtual Sequence Animate(ILootItem loot, ICollectable collectable, ILootCollector collector)
        {
            return null;
        }
    }
}