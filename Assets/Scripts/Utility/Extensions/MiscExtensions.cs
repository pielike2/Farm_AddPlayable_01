using System;
using System.Collections.Generic;
using Base;
using UnityEngine;

namespace Utility.Extensions
{
    public static class MiscExtensions
    {
        public static T GetBySafeIndex<T>(this T[] array, int index)
        {
            if (array == null || array.Length == 0) return default;
            if (index < 0) return array[0];
            if (index >= array.Length) return array[array.Length - 1];
    
            return array[index];
        }

        public static bool IsIndexSafe<T>(this T[] array, int index)
        {
            return index >= 0 && index < array.Length;
        }
        
        public static Color SetAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static string SetColorTag(this string s, string color)
        {
            return string.IsNullOrEmpty(s) ? s : $"<color={color}>{s}</color>";
        }

        public static Transform FindDeepChild(this Transform parent, string name, bool includingSelf = true, bool includingInactive = false)
        {
            if (includingSelf && parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                if (child.name == name && (child.gameObject.activeInHierarchy || includingInactive))
                    return child;
                var result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }

            return null;
        }

#if UNITY_EDITOR
        public static string GetHierarchyPath(this Transform transform)
        {
            var path = "";
            var current = transform;
            while (current != null)
            {
                if (current.parent == null)
                    path += current.name;
                else
                    path += "/" + current.name;
                
                current = current.parent;
            }
            return path;
        }
#endif

        public static Vector3 ToAngularVelocity(this Quaternion q)
        {
            if ( Mathf.Abs(q.w) > 1023.5f / 1024.0f)
                return new Vector3();
            var angle = Mathf.Acos( Mathf.Abs(q.w) );
            var gain = Mathf.Sign(q.w)*2.0f * angle / Mathf.Sin(angle);
 
            return new Vector3(q.x * gain, q.y * gain, q.z * gain);
        }
        
        public static Quaternion FromAngularVelocity(this Vector3 w)
        {
            var mag = w.magnitude;
            if (mag <= 0)
                return Quaternion.identity;
            var cs = Mathf.Cos(mag * 0.5f);
            var siGain = Mathf.Sin(mag * 0.5f) / mag;
            return new Quaternion(w.x * siGain, w.y * siGain, w.z * siGain, cs);
        }

        public static Vector3 PerAxisMult(this Vector3 v1, Vector3 v2)
        {
            return new Vector3(
                v1.x * v2.x, 
                v1.y * v2.y, 
                v1.z * v2.z);
        }

        public static Vector3 DirectionTo(this Vector3 v1, Vector3 v2)
        {
            return (v2 - v1).normalized;
        }
        
        public static Vector3 DirectionFrom(this Vector3 v1, Vector3 v2)
        {
            return (v1 - v2).normalized;
        }

        public static Quaternion RotationTo(this Vector3 v1, Vector3 v2)
        {
            return Quaternion.LookRotation(v2 - v1);
        }

        public static Quaternion RotationFrom(this Vector3 v1, Vector3 v2)
        {
            return Quaternion.LookRotation(v1 - v2);
        }
        
        public static T AddTo<T>(this T disposable, ICollection<IDisposable> container)
            where T : IDisposable
        {
            container.Add(disposable);
            return disposable;
        }

        public static void ZeroLocals(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void Toggle(this AudioSource audioSource, bool active)
        {
            if (active && !audioSource.isPlaying)
                audioSource.Play();
            else if (!active && audioSource.isPlaying)
                audioSource.Stop();
        }

        public static void Toggle(this ParticleSystem ps, bool active)
        {
            if (active && !ps.isPlaying)
                ps.Play();
            else if (!active && ps.isPlaying)
                ps.Stop();
        }
    }
}