using System;
using UnityEngine;
using Utility;

namespace Base.PoolingSystem
{
    public class CommonObjectPool : MonoSingleton<CommonObjectPool>
    {
        private Transform _root;
        private MonoExpandablePool _pool;

        protected override void Init()
        {
            base.Init();

            _root = new GameObject("[CommonPool]").transform;
            _pool = new MonoExpandablePool(_root);
        }

        public void Warm(IMonoPoolObject prefab, int count = 1)
        {
            _pool.Warm(prefab, count);
        }

        public T Spawn<T>(T prefab, Vector3 pos, Quaternion rotation) where T : class, IMonoPoolObject
        {
            return _pool.Spawn(prefab, pos, rotation);
        }

        public bool Release(IMonoPoolObject clone)
        {
            return _pool.Release(clone);
        }

        public void ReleaseAll()
        {
            _pool.ReleaseAll();
        }

        public void AddReleaseHandler(IMonoPoolObject obj, Action<IMonoPoolObject> action)
        {
            _pool.AddReleaseHandler(obj, action);
        }
    }
}