using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utility.Extensions
{
    public static class MonoRoutineExtensions
    {
        public static Coroutine After(this MonoBehaviour beh, float delay, Action action)
        {
            return beh != null && beh.isActiveAndEnabled ? beh.StartCoroutine(DelayRoutine(beh, delay, action)) : null;
        }
        
        public static Coroutine After<T>(this T beh, float delay, Action<T> action) where T : MonoBehaviour
        {
            return beh != null && beh.isActiveAndEnabled ? beh.StartCoroutine(DelayRoutine(beh, delay, action)) : null;
        }
        
        public static Coroutine AfterRealtime(this MonoBehaviour beh, float delay, Action action)
        {
            return beh != null && beh.isActiveAndEnabled ? beh.StartCoroutine(DelayRealtimeRoutine(beh, delay, action)) : null;
        }

        public static Coroutine NextUpdate(this MonoBehaviour beh, Action action)
        {
            return beh != null && beh.isActiveAndEnabled ? beh.StartCoroutine(FrameUpdateCoroutine(beh, action)) : null;
        }

        public static Coroutine NextFixedUpdate(this MonoBehaviour beh, Action action)
        {
            return beh != null && beh.isActiveAndEnabled ? beh.StartCoroutine(FixedUpdateRoutine(beh, action)) : null;
        }

        static IEnumerator FixedUpdateRoutine(MonoBehaviour beh, Action action)
        {
            yield return new WaitForFixedUpdate();
            if (action != null && beh != null)
                action();
        }

        static IEnumerator FrameUpdateCoroutine(MonoBehaviour beh, Action action)
        {
            yield return null;
            if (action != null && beh != null)
                action();
        }

        static IEnumerator DelayRoutine(Object beh, float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            if (action != null && beh != null)
                action();
        }

        static IEnumerator DelayRoutine<T>(T beh, float delay, Action<T> action) where T : Object
        {
            yield return new WaitForSeconds(delay);
            if (action != null && beh != null)
                action(beh);
        }

        static IEnumerator DelayRealtimeRoutine(Object beh, float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (action != null && beh != null)
                action();
        }
    }
}