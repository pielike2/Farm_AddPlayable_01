using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playable.Animations
{
    public class FollowTweenAnimation : BaseMovementTweenAnimation
    {
        public float followDuration = 1.0f;

        public override float BasicDuration => followDuration;

        public override Sequence Animate(Transform transform, Transform targetTransform, float startDelay = 0,
            float customDuration = -1f)
        {
            var seq = DOTween.Sequence();

            transform.position = targetTransform.position;

            var followTween = DOVirtual.Float(0, 1, customDuration > 0f 
                    ? customDuration : followDuration, _ => { })
                .OnUpdate(() =>
                {
                    if (transform == null || targetTransform == null)
                    {
                        seq.Kill();
                        return;
                    }
                    
                    transform.position = targetTransform.position;
                });

            seq.Append(followTween);
            return seq;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FollowTweenAnimation))]
    [CanEditMultipleObjects]
    public class FollowTweenAnimationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("followDuration"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}