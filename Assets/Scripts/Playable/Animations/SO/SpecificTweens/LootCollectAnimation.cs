using DG.Tweening;
using Playable.Gameplay.Collectables;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Animations
{
    public class LootCollectAnimation : BaseLootCollectAnimation
    {
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
        [SerializeField] private AnimationCurve _jumpScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float _jumpDuration = 0.3f;
        [SerializeField] private float _jumpTiltAngle = 45f;
        
        [SerializeField] private AnimationCurve _movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _movementDuration = 0.5f;
        [SerializeField] private AnimationCurve _movementScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.5f);
        
        public override Sequence Animate(ILootItem loot, ICollectable collectable, ILootCollector collector)
        {
            var seq = DOTween.Sequence();
            var t = loot.transform;
            
            // Jump from source
            seq.Append(t.DOMoveY(1f, _jumpDuration).SetRelative().SetEase(_jumpCurve));
            seq.Join(ApplyScale(t, _jumpScaleCurve, _jumpDuration));
            
            var tiltAxis = Vector3.Cross(Vector3.up, (loot.transform.position - collectable.transform.position).normalized);
            seq.Join(t.DOBlendableRotateBy(tiltAxis * _jumpTiltAngle, _jumpDuration).SetEase(Ease.InCubic));
        
            // Move to collector
            var startPos = t.position;
            seq.Append(DOVirtual.Float(0f, 1f, _movementDuration,
                v => t.position = Vector3.Lerp(startPos, collector.GetLootCollectPoint(0).position, v)).SetEase(_movementCurve));
            seq.Join(ApplyScale(t, _movementScaleCurve, _movementDuration));

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