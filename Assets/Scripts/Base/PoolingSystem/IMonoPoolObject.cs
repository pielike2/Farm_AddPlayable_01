using UnityEngine;
using Utility;

namespace Base.PoolingSystem
{
    public interface IMonoPoolObject : IUnityObject
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}