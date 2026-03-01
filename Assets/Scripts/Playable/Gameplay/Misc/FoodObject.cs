using System;
using Base.PoolingSystem;
using DG.Tweening;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Misc
{
    public interface IFoodObject : IMonoPoolObject, IUnityObject
    {
        float EatenPercent { get; }
        float RemainingPercent { get; }
        bool IsEaten { get; }
        void Eat(float percent);
    }

    public class FoodObject : MonoBehaviour, IMonoPoolObject, IFoodObject
    {
        [Serializable]
        public class FoodObjectState
        {
            public Vector3 scale = Vector3.one;
        }
        
        [SerializeField] private FoodObjectState[] _states = new FoodObjectState[] {new FoodObjectState {scale = Vector3.one}};
        [SerializeField] private float _baseAnimDuration = 0.3f;

        private int _currentState;
        
        public float EatenPercent { get; private set; }
        public float RemainingPercent => 1f - EatenPercent;
        public bool IsEaten => EatenPercent > 0.99999f;
        
        public void OnSpawnFromPool()
        {
            EatenPercent = 0f;
        }

        public void OnReturnToPool()
        {
            transform.localScale = _states[0].scale;
            _currentState = 0;
        }

        public void Eat(float percent)
        {
            if (IsEaten)
                return;
            
            EatenPercent += percent;
            EatenPercent = Mathf.Clamp01(EatenPercent);
            
            var prevState = _states[_currentState];
            var ind = Mathf.FloorToInt(EatenPercent * (_states.Length - 1));
            if (ind < 0 || ind >= _states.Length)
                return;
            
            _currentState = ind;
            var newState = _states[ind];
            Animate(prevState, newState, percent);
        }

        private void Animate(FoodObjectState prevState, FoodObjectState newState, float eatenPercent)
        {
            transform.DOKill();
            
            var duration = _baseAnimDuration * (1f + eatenPercent * 0.75f);
            var tween = transform.DOScale(newState.scale, duration).SetEase(Ease.InQuart);

            if (IsEaten)
                tween.onComplete += this.Release;
        }
    }
}