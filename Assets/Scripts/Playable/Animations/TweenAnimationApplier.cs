using DG.Tweening;
using UnityEngine;

namespace Playable.Animations
{
    public class TweenAnimationApplier : MonoBehaviour
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private BaseSimpleTweenAnimation _animation;
        
        [Space(5)]
        [SerializeField] private bool _loop;
        
        [Space(5)]
        [SerializeField] private bool _resetPosition = true;
        [SerializeField] private bool _resetRotation = true;
        [SerializeField] private bool _resetScale = true;

        private Sequence _sequence;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialPosition = _transform.localPosition;
            _initialRotation = _transform.localRotation;
            _initialScale = _transform.localScale;
        }

        private void Reset()
        {
            _transform = transform;
        }

        private void OnEnable()
        {
            ResetTransform();
            PlayAnimation();
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            
            ResetTransform();
        }

        private void ResetTransform()
        {
            if (_resetPosition)
                _transform.localPosition = _initialPosition;
            if (_resetRotation)
                _transform.localRotation = _initialRotation;
            if (_resetScale)
                _transform.localScale = _initialScale;
        }

        public void StopAnimation()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void PlayAnimation()
        {
            _sequence = _animation.Animate(_transform);
            if (_loop)
                _sequence.SetLoops(-1, LoopType.Restart);
        }
    }
}