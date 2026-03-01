using UnityEngine;

namespace Utility
{
    public interface IUnityObject
    {
        GameObject gameObject { get; }
        Transform transform { get; }
    }
}