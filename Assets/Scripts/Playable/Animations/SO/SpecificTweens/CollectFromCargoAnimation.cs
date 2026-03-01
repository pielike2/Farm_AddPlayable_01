using DG.Tweening;
using UnityEngine;

namespace Playable.Animations
{
    public class CollectFromCargoAnimation : ScriptableObject
    {
        [Header("Jump")]
        [SerializeField] private float _jumpDuration = 0.3f;
        [SerializeField] private float _jumpHeight = 0.5f;
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
        [SerializeField] private float _jumpSideOffset = 0.5f;
        [SerializeField] private AnimationCurve _jumpSideOffsetCurve = AnimationCurve.EaseInOut(0, 0, 0, 0);
        
        [SerializeField] private AnimationCurve _jumpTiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _jumpScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float _jumpTiltAngle = 45f;
        
        [Header("Movement")]
        [SerializeField] private float _movementDuration = 0.5f;
        [SerializeField] private AnimationCurve _movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _movementHeightOffset = 0.5f;
        [SerializeField] private AnimationCurve _movementHeightOffsetCurve = AnimationCurve.EaseInOut(0, 0, 0, 0);
        [SerializeField] private AnimationCurve _movementScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.5f);

        public Sequence Animate(Transform t, Transform target, Vector3 centerPoint, Transform character, float jumpDurationMult = 1f)
        {
            var seq = DOTween.Sequence();
            var jumpDuration = _jumpDuration * jumpDurationMult;

            var sideDir = (t.position - centerPoint).normalized;
            
            var jumpStartPos = t.position;
            seq.Append(DOVirtual.Float(0f, 1f, jumpDuration, time =>
            {
                var heightOffset = Vector3.up * (_jumpCurve.Evaluate(time) * _jumpHeight);
                var sideOffset = sideDir * (_jumpSideOffsetCurve.Evaluate(time) * _jumpSideOffset);
                t.position = jumpStartPos + heightOffset + sideOffset;;
            }).SetTarget(t).SetEase(Ease.Linear));
            
            seq.Join(ApplyScale(t, _jumpScaleCurve, jumpDuration));
            
            var tiltAxis = Vector3.Cross(Vector3.up, (t.position - centerPoint).normalized);
            seq.Join(t.DOBlendableRotateBy(tiltAxis * _jumpTiltAngle, jumpDuration).SetEase(_jumpTiltCurve));

            // Move to collector
            seq.AppendCallback(() =>
            {
                var moveStartPos = t.position;
                var moveSeq = DOTween.Sequence();
                moveSeq.Append(DOVirtual.Float(0f, 1f, _movementDuration,
                    time =>
                    {
                        var pos = Vector3.Lerp(moveStartPos, target.position, _movementCurve.Evaluate(time));
                        pos += Vector3.up * (_movementHeightOffsetCurve.Evaluate(time) * _movementHeightOffset);
                        t.position = pos;
                    }).SetEase(Ease.Linear)).SetTarget(t);
                moveSeq.Join(ApplyScale(t, _movementScaleCurve, _movementDuration));
            });

            seq.AppendInterval(_movementDuration);

            seq.SetTarget(t);

            return seq;
        }

        private static Tween ApplyScale(Transform t, AnimationCurve curve, float duration)
        {
            var originalScale = t.localScale;
            
            var tween = DOVirtual.Float(0f, 1f, duration, time =>
            {
                var v = curve.Evaluate(time);
                t.localScale = new Vector3(
                    originalScale.x * v,
                    originalScale.y * v,
                    originalScale.z * v);
            }).SetTarget(t).SetEase(Ease.Linear);

            return tween;
        }
    }
}