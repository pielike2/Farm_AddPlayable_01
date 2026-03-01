using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Base.PoolingSystem
{
    public class SingleMonoPool
    {
        private readonly Transform _container;
        private readonly List<IMonoPoolObject> _spawnedClones = new List<IMonoPoolObject>();
        private readonly List<IMonoPoolObject> _pooledClones = new List<IMonoPoolObject>();
        private readonly IMonoPoolObject _prefab;
        
        public event Action<SingleMonoPool, IMonoPoolObject> OnWarm;

        public SingleMonoPool(Transform container, IMonoPoolObject prefab)
        {
            _container = container;
            _prefab = prefab;
        }

        public void Warm()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab as MonoBehaviour, _container) as IMonoPoolObject;
            _pooledClones.Add(instance);
            OnWarm?.Invoke(this, instance);
            instance.gameObject.SetActive(false);
        }

        public IMonoPoolObject Spawn()
        {
            if (_pooledClones.Count == 0)
            {
                Warm();
            }

            var cloneToSpawn = _pooledClones[0];
            _pooledClones.RemoveAt(0);
            _spawnedClones.Add(cloneToSpawn);

            cloneToSpawn.gameObject.SetActive(true);

            return cloneToSpawn;
        }

        public bool Release(IMonoPoolObject clone)
        {
            if (_pooledClones.Contains(clone))
            {
                //Debug.LogWarning($"Clone already in pool: {clone.gameObject.name}");
                return false;
            }
            _spawnedClones.Remove(clone);
            _pooledClones.Add(clone);

            clone.OnReturnToPool();
            clone.gameObject.SetActive(false);

            DOTween.Kill(clone);
            DOTween.Kill(clone.transform);

            if (_container != null)
                clone.gameObject.transform.SetParent(_container, false);

            // clone.transform.localPosition = _prefab.transform.localPosition;
            // clone.transform.localRotation = _prefab.transform.localRotation;
            clone.transform.localScale = _prefab.transform.localScale;

            return true;
        }

        public void ReleaseAll()
        {
            while (_spawnedClones.Count > 0)
                Release(_spawnedClones.First());
        }
    }
}
