using DG.Tweening;
using UnityEngine;
using Utility;

namespace Playable.Animations
{
    public class BaseMovementTweenAnimation : ScriptableObject
    {
        public float[] customCallbackTimes;
        
        public virtual float BasicDuration => 0f;
        public float CustomDuration { get; private set; }
        public bool CustomDurationEnabled { get; private set; }
        
        public virtual Sequence Animate(Transform transform, Vector3 targetPos, float startDelay = 0f, float customDuration = -1f)
        {
            return null;
        }
        
        public virtual Sequence Animate(Transform transform, Transform targetTransform, float startDelay = 0f, float customDuration = -1f)
        {
            return null;
        }

        public static Tween ApplyMovement(float duration, Transform transform, Vector3 targetPos,
            AnimationCurve movementCurve, AxisCurveSet movementCurveSet, bool separateAxes, bool offsetEnabled, Vector3 offset, AxisCurveSet offsetCurve)
        {
            var startPos = transform.position;
            return DOVirtual.Float(0f, 1f, duration, time =>
            {
                var pos = new Vector3();
                if (separateAxes)
                {
                    pos.x = Mathf.Lerp(startPos.x, targetPos.x, movementCurveSet.X.Evaluate(time));
                    pos.y = Mathf.Lerp(startPos.y, targetPos.y, movementCurveSet.Y.Evaluate(time));
                    pos.z = Mathf.Lerp(startPos.z, targetPos.z, movementCurveSet.Z.Evaluate(time));
                }
                else
                {
                    pos = Vector3.Lerp(startPos, targetPos, movementCurve.Evaluate(time));
                }
                if (offsetEnabled)
                {
                    pos += new Vector3(
                        offset.x * offsetCurve.X.Evaluate(time), 
                        offset.y * offsetCurve.Y.Evaluate(time),
                        offset.z * offsetCurve.Z.Evaluate(time));
                }
                transform.position = pos;
            }).SetTarget(transform);
        }

        public static Tween ApplyMovement(float duration, Transform transform, Transform targetTransform,
            AnimationCurve movementCurve, AxisCurveSet movementCurveSet, bool separateAxes, bool offsetEnabled, Vector3 offset, AxisCurveSet offsetCurve)
        {
            var startPos = transform.position;
            return DOVirtual.Float(0f, 1f, duration, time =>
            {
                var pos = new Vector3();
                if (separateAxes)
                {
                    pos.x = Mathf.Lerp(startPos.x, targetTransform.position.x, movementCurveSet.X.Evaluate(time));
                    pos.y = Mathf.Lerp(startPos.y, targetTransform.position.y, movementCurveSet.Y.Evaluate(time));
                    pos.z = Mathf.Lerp(startPos.z, targetTransform.position.z, movementCurveSet.Z.Evaluate(time));
                }
                else
                {
                    pos = Vector3.Lerp(startPos, targetTransform.position, movementCurve.Evaluate(time));
                }
                if (offsetEnabled)
                {
                    pos += new Vector3(
                        offset.x * offsetCurve.X.Evaluate(time), 
                        offset.y * offsetCurve.Y.Evaluate(time),
                        offset.z * offsetCurve.Z.Evaluate(time));
                }
                transform.position = pos;
            }).SetTarget(transform);
        }
    }
}