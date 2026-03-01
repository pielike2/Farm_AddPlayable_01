using DG.Tweening;
using UnityEngine;

namespace Utility.Extensions
{
    public static class AxisCurveSetDotweenExtensions
    {
        public static Tween ApplyOffset(this AxisCurveSet set, Vector3 offset, Transform t, float duration, UpdateType updateType = UpdateType.Normal)
        {
            var dynamicOffset = new Vector3(
                offset.x * set.X.Evaluate(0f),
                offset.y * set.Y.Evaluate(0f),
                offset.z * set.Z.Evaluate(0f));
            t.position += dynamicOffset;
            
            var tween = DOVirtual.Float(0f, 1f, duration, time =>
            {
                t.position -= dynamicOffset;
                dynamicOffset.x = offset.x * set.X.Evaluate(time);
                dynamicOffset.y = offset.y * set.Y.Evaluate(time);
                dynamicOffset.z = offset.z * set.Z.Evaluate(time);
                t.position += dynamicOffset;
            }).SetTarget(t).SetEase(Ease.Linear).SetUpdate(updateType);
        
            return tween;
        }

        public static void ApplyOffset(this AxisCurveSet set, Sequence seq, Vector3 offset, Transform t, float duration, UpdateType updateType = UpdateType.Normal)
        {
            if (offset.x != 0f)
                seq.Join(t.DOBlendableMoveBy(new Vector3(offset.x, 0f, 0f), duration).SetEase(set.X)).SetUpdate(updateType);
            if (offset.y != 0f)
                seq.Join(t.DOBlendableMoveBy(new Vector3(0f, offset.y, 0f), duration).SetEase(set.Y)).SetUpdate(updateType);
            if (offset.z != 0f)
                seq.Join(t.DOBlendableMoveBy(new Vector3(0f, 0f, offset.z), duration).SetEase(set.Z)).SetUpdate(updateType);
        }
        
        public static Tween ApplyScale(this AxisCurveSet set, Transform t, float duration, UpdateType updateType = UpdateType.Normal)
        {
            var originalScale = t.localScale;
            t.localScale =  new Vector3(
                originalScale.x * set.X.Evaluate(0f),
                originalScale.y * set.Y.Evaluate(0f),
                originalScale.z * set.Z.Evaluate(0f));
            
            var tween = DOVirtual.Float(0f, 1f, duration, v =>
            {
                t.localScale = new Vector3(
                    originalScale.x * set.X.Evaluate(v),
                    originalScale.y * set.Y.Evaluate(v),
                    originalScale.z * set.Z.Evaluate(v));
            }).SetTarget(t).SetEase(Ease.Linear).SetUpdate(updateType);
            
            return tween;
        }
        
        public static Tween ApplyScale(this AxisCurveSet set, Vector3 customScale, Transform t, float duration, UpdateType updateType = UpdateType.Normal)
        {
            t.localScale =  new Vector3(
                customScale.x * set.X.Evaluate(0f),
                customScale.y * set.Y.Evaluate(0f),
                customScale.z * set.Z.Evaluate(0f));
            
            var tween = DOVirtual.Float(0f, 1f, duration, v =>
            {
                t.localScale = new Vector3(
                    customScale.x * set.X.Evaluate(v),
                    customScale.y * set.Y.Evaluate(v),
                    customScale.z * set.Z.Evaluate(v));
            }).SetTarget(t).SetEase(Ease.Linear).SetUpdate(updateType);
            
            return tween;
        }
        
        public static Sequence ApplyRotate(this AxisCurveSet set, Vector3 rotation, Transform t, float duration, UpdateType updateType = UpdateType.Normal)
        {
            var seq = DOTween.Sequence();
            if (rotation.x != 0f)
                seq.Join(t.DOBlendableLocalRotateBy(new Vector3(rotation.x, 0f, 0f), duration).SetEase(set.X));
            if (rotation.y != 0f)
                seq.Join(t.DOBlendableLocalRotateBy(new Vector3(0f, rotation.y, 0f), duration).SetEase(set.Y));
            if (rotation.z != 0f)
                seq.Join(t.DOBlendableLocalRotateBy(new Vector3(0f, 0f, rotation.z), duration).SetEase(set.Z));
            seq.SetUpdate(updateType);
            return seq;
        }
    }
}