using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Base.PoolingSystem
{
    public static class ObjectPoolExtensions
    {
        public static T Spawn<T>(this T prefab, Vector3 pos, Quaternion rotation) where T : class, IMonoPoolObject
        {
            if (CommonObjectPool.Instance != null && CommonObjectPool.AwakeDone)
                return CommonObjectPool.Instance.Spawn(prefab, pos, rotation);

            return Object.Instantiate(prefab as MonoBehaviour, pos, rotation) as T;
        }
        
        public static T Spawn<T>(this T prefab, Vector3 pos, Vector3 rotation) where T : class, IMonoPoolObject
        {
            return Spawn(prefab, pos, Quaternion.Euler(rotation));
        }
        
        public static T Spawn<T>(this T prefab, Vector3 pos) where T : class, IMonoPoolObject
        {
            return Spawn(prefab, pos, Quaternion.identity);
        }
        
        public static T Spawn<T>(this T prefab) where T : class, IMonoPoolObject
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity);
        }

        public static void Release(this IMonoPoolObject obj)
        {
            if (CommonObjectPool.Instance != null && CommonObjectPool.AwakeDone)
            {
                var success = CommonObjectPool.Instance.Release(obj);
                if (!success && obj.gameObject.activeSelf)
                {
                    if (Application.isPlaying)
                        Object.Destroy(obj.gameObject);
                    else
                        Object.DestroyImmediate(obj.gameObject);
                }
            }
            else
            {
                if (!Application.isPlaying)
                    Object.DestroyImmediate(obj.gameObject);
            }
        }

        public static void AddOnReleaseHandler(this IMonoPoolObject obj, Action<IMonoPoolObject> action)
        {
            if (!CommonObjectPool.HasInstance)
                return;
            CommonObjectPool.Instance.AddReleaseHandler(obj, action);
        }
    }
}
