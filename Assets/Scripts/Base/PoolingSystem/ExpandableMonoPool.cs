using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base.PoolingSystem
{
    public class MonoExpandablePool
    {
        public Transform Container { get; }

        private readonly Dictionary<IMonoPoolObject, SingleMonoPool> _poolLookup =
            new Dictionary<IMonoPoolObject, SingleMonoPool>();
        private readonly Dictionary<IMonoPoolObject, SingleMonoPool> _cloneLookup =
            new Dictionary<IMonoPoolObject, SingleMonoPool>();
        private readonly Dictionary<IMonoPoolObject, HashSet<Action<IMonoPoolObject>>> _onReleaseEvents =
            new Dictionary<IMonoPoolObject, HashSet<Action<IMonoPoolObject>>>();

        public event Action<IMonoPoolObject> OnAfterRelease;

        public MonoExpandablePool(Transform container)
        {
            Container = container;
        }

        public void Warm(IMonoPoolObject prefab, int count = 1)
        {
            var pool = !_poolLookup.TryGetValue(prefab, out var existingPool) ? CreatePoolForPrefab(prefab) : existingPool;
            for (int i = 0; i < count; i++)
                pool.Warm();
        }

        public T Spawn<T>(T prefab, Vector3 pos, Quaternion rotation) where T : IMonoPoolObject
        {
            var pool = !_poolLookup.TryGetValue(prefab, out var existingPool) ? CreatePoolForPrefab(prefab) : existingPool;
            var clone = (T)pool.Spawn();

            clone.transform.position = pos;
            clone.transform.rotation = rotation;
            clone.transform.localScale = prefab.transform.localScale;

            clone.OnSpawnFromPool();

            return clone;
        }

        public bool Release(IMonoPoolObject clone)
        {
            if (!_cloneLookup.ContainsKey(clone))
                return false;
            
            if (_onReleaseEvents.ContainsKey(clone))
            {
                foreach (var action in _onReleaseEvents[clone])
                    action(clone);
                _onReleaseEvents[clone].Clear();
                // _onReleaseEvents.Remove(clone);
            }
            var success = _cloneLookup[clone].Release(clone);

            OnAfterRelease?.Invoke(clone);

            return success;
        }

        public void ReleaseAll()
        {
            foreach (var pair in _cloneLookup)
                pair.Value.ReleaseAll();
        }

        public void AddReleaseHandler(IMonoPoolObject obj, Action<IMonoPoolObject> action)
        {
            if (!_onReleaseEvents.ContainsKey(obj))
                _onReleaseEvents.Add(obj, new HashSet<Action<IMonoPoolObject>>());
            _onReleaseEvents[obj].Add(action);
        }

        private IMonoPoolObject InstantiateFromPrefab(IMonoPoolObject prefab)
        {
            var go = UnityEngine.Object.Instantiate(prefab.gameObject, Container, false);
            if (Container != null)
                go.transform.SetParent(Container, false);
            return go.GetComponent<IMonoPoolObject>();
        }

        private SingleMonoPool CreatePoolForPrefab(IMonoPoolObject prefab)
        {
            var pool = new SingleMonoPool(Container, prefab);
            pool.OnWarm += OnPoolWarm;
            _poolLookup.Add(prefab, pool);
            return pool;
        }

        private void OnPoolWarm(SingleMonoPool pool, IMonoPoolObject obj)
        {
            _cloneLookup.Add(obj, pool);
        }
    }
}
