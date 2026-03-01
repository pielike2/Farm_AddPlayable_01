using DG.Tweening;
using UnityEngine;

namespace Playable.Animations
{
    public class BaseSimpleTweenAnimation : ScriptableObject
    {
        public float[] customCallbackTimes;
        
        public virtual float Duration => 0f;
        
        public virtual Sequence Animate(Transform transform, float startDelay = 0f)
        {
            return null;
        }
    }
}