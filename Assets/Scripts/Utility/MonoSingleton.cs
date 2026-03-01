using System;
using UnityEngine;

namespace Utility
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = (T)FindObjectOfType(typeof(T));
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;
        public static bool AwakeDone { get; private set; }
        public static event Action OnAwakeDone;
        
        protected virtual void Awake()
        {
            _instance = this as T;
            AwakeDone = true;
            OnAwakeDone?.Invoke();
            OnAwakeDone = null;
            Init();
        }

        protected virtual void Init() { }
    }
}