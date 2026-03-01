using DG.Tweening;
using UnityEngine;
using Utility;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playable.Animations
{
    public class SimpleMovementTweenAnimation : BaseMovementTweenAnimation
    {
        public float duration = 0.5f;

        [Space(10)] 
        public bool separateAxes;
        public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AxisCurveSet movementCurveSet = AxisCurveSet.Linear(0, 0, 1, 1);

        public override float BasicDuration => duration;

        public override Sequence Animate(Transform transform, Vector3 targetPos, float startDelay = 0f, float customDuration = -1f)
        {
            var dur = customDuration >= 0f ? customDuration : duration;
            var seq = DOTween.Sequence();
            
            seq.Join(ApplyMovement(dur, transform, targetPos, movementCurve, movementCurveSet, separateAxes, false, Vector3.zero, null));

            if (startDelay > 0f)
                seq.PrependInterval(startDelay);

            seq.SetTarget(transform);
            
            return seq;
        }

        public override Sequence Animate(Transform transform, Transform targetTransform, float startDelay = 0, float customDuration = -1f)
        {
            var dur = customDuration >= 0f ? customDuration : duration;
            var seq = DOTween.Sequence();
            
            seq.Join(ApplyMovement(dur, transform, targetTransform, movementCurve, movementCurveSet, separateAxes, false, Vector3.zero, null));

            if (startDelay > 0f)
                seq.PrependInterval(startDelay);

            seq.SetTarget(transform);
            
            return seq;
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(SimpleMovementTweenAnimation))]
    [CanEditMultipleObjects]
    public class SimpleMovementTweenAnimationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customCallbackTimes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("separateAxes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("separateAxes").boolValue
                ? serializedObject.FindProperty("movementCurveSet")
                : serializedObject.FindProperty("movementCurve"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}