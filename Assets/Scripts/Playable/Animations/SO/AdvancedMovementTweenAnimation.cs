using System;
using DG.Tweening;
using UnityEngine;
using Utility;
using Utility.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playable.Animations
{
    public class AdvancedMovementTweenAnimation : BaseMovementTweenAnimation
    {
        public enum DurationType
        {
            Fixed, // Mode 1
            BySpeed // Mode 2
        }

        [Serializable]
        public class DurationSettings
        {
            public DurationType durationMode;
            
            public float mode1_duration = 0.5f;
            
            public float mode2_speed = 5f;
        }

        public DurationSettings durationSettings;

        public override float BasicDuration => durationSettings.mode1_duration;

        [Space(10)] 
        public bool separateAxes;
        public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AxisCurveSet movementCurveSet = AxisCurveSet.Linear(0, 0, 1, 1);

        [Space(10)] 
        public bool scaleEnabled;
        public bool useCustomScale;
        public Vector3 customScale = Vector3.one;
        public AxisCurveSet scaleCurve = AxisCurveSet.EaseInOut(0, 1, 1, 1);

        [Space(10)]
        public bool rotationEnabled;
        public Vector3 rotation = new Vector3(45, 0, 0);
        public AxisCurveSet rotationCurve = AxisCurveSet.EaseInOut(0, 0, 1, 0);
        
        [Space(10)]
        public bool offsetEnabled;
        public Vector3 offset = new Vector3(0, 0.5f, 0);
        public AxisCurveSet offsetCurve = AxisCurveSet.EaseInOut(0, 0, 1, 0);
        

        public override Sequence Animate(Transform transform, Vector3 targetPos, float startDelay = 0f, float customDuration = -1f)
        {
            var dur = customDuration >= 0f ? customDuration : GetDuration(transform, targetPos);
            var seq = DOTween.Sequence();
            seq.Join(ApplyMovement(dur, transform, targetPos, movementCurve, movementCurveSet, separateAxes, offsetEnabled, offset, offsetCurve));
            ApplySecondary(transform, startDelay, seq, dur);
            seq.SetTarget(transform);
            return seq;
        }

        public override Sequence Animate(Transform transform, Transform targetTransform, float startDelay = 0, float customDuration = -1f)
        {
            var duration = customDuration >= 0f ? customDuration : GetDuration(transform, targetTransform.position);
            var seq = DOTween.Sequence();
            seq.Join(ApplyMovement(duration, transform, targetTransform, movementCurve, movementCurveSet, separateAxes, offsetEnabled, offset, offsetCurve));
            ApplySecondary(transform, startDelay, seq, duration);
            seq.SetTarget(transform);
            return seq;
        }

        private float GetDuration(Transform transform, Vector3 targetPos)
        {
            switch (durationSettings.durationMode)
            {
                case DurationType.Fixed:
                    return durationSettings.mode1_duration;
                case DurationType.BySpeed:
                    return Vector3.Distance(transform.position, targetPos) / durationSettings.mode2_speed;
            }
            return BasicDuration;
        }

        private void ApplySecondary(Transform transform, float startDelay, Sequence seq, float dur)
        {
            if (scaleEnabled)
            {
                if (useCustomScale)
                    seq.Join(scaleCurve.ApplyScale(customScale, transform, dur));
                else
                    seq.Join(scaleCurve.ApplyScale(transform, dur));
            }
            
            if (rotationEnabled)
                seq.Join(rotationCurve.ApplyRotate(rotation, transform, dur));
            
            if (offsetEnabled)
                seq.Join(offsetCurve.ApplyOffset(offset, transform, dur));

            if (startDelay > 0f)
                seq.PrependInterval(startDelay);
            
            seq.SetTarget(transform);
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(AdvancedMovementTweenAnimation))]
    [CanEditMultipleObjects]
    public class AdvancedMovementTweenAnimationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var tween = (AdvancedMovementTweenAnimation) target;
            var durationSettingsProp = serializedObject.FindProperty("durationSettings");
            
            EditorGUILayout.PropertyField(durationSettingsProp.FindPropertyRelative("durationMode"));
            switch (tween.durationSettings.durationMode)
            {
                case AdvancedMovementTweenAnimation.DurationType.Fixed:
                    EditorGUILayout.PropertyField(durationSettingsProp.FindPropertyRelative("mode1_duration"), new GUIContent("Duration"));
                    break;
                case AdvancedMovementTweenAnimation.DurationType.BySpeed:
                    EditorGUILayout.PropertyField(durationSettingsProp.FindPropertyRelative("mode2_speed"), new GUIContent("Speed"));
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("separateAxes"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("separateAxes").boolValue
                ? serializedObject.FindProperty("movementCurveSet")
                : serializedObject.FindProperty("movementCurve"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleEnabled"));
            if (serializedObject.FindProperty("scaleEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useCustomScale"));
                if (serializedObject.FindProperty("useCustomScale").boolValue)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleCurve"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationEnabled"));
            if (serializedObject.FindProperty("rotationEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationCurve"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetEnabled"));
            if (serializedObject.FindProperty("offsetEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetCurve"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
}