using DG.Tweening;
using UnityEngine;
using Utility;
using Utility.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playable.Animations
{
    public class SimpleTweenAnimation : BaseSimpleTweenAnimation
    {
        public float duration = 0.5f;
        public float delay = 0f;

        public override float Duration => duration;

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

        public override Sequence Animate(Transform transform, float startDelay = 0f)
        {
            var seq = DOTween.Sequence().SetTarget(transform);
            
            if (scaleEnabled)
            {
                if (useCustomScale)
                    seq.Join(scaleCurve.ApplyScale(customScale, transform, duration));
                else
                    seq.Join(scaleCurve.ApplyScale(transform, duration));
            }
            if (rotationEnabled)
                seq.Join(rotationCurve.ApplyRotate(rotation, transform, duration));
            if (offsetEnabled)
                seq.Join(offsetCurve.ApplyOffset(offset, transform, duration));

            if (startDelay > 0f || delay > 0f)
                seq.PrependInterval(startDelay + delay);

            return seq;
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(SimpleTweenAnimation))]
    [CanEditMultipleObjects]
    public class SimpleTweenAnimationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customCallbackTimes"));
            
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