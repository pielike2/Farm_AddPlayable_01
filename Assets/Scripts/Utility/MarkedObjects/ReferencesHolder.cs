using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utility
{
    public class ReferencesHolder : MonoBehaviour
    {
        [Serializable]
        public class Reference
        {
            public string id;
            public Object @object;
        }

        [SerializeField] private Reference[] _references;

        private bool _isInitialized; 
        private Dictionary<int, Object> _objects = new Dictionary<int, Object>();

        private void TryInit()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;

            foreach (var item in _references)
                _objects[HashUtil.StringToHash(item.id)] = item.@object;
        }

        public Object GetObject(string id)
        {
            return GetObject(HashUtil.StringToHash(id));
        }

        public Object GetObject(int id)
        {
            TryInit();
            return _objects.GetValueOrDefault(id);
        }

        public T GetObject<T>(string id) where T : Object
        {
            var obj = GetObject(id);
            return obj != null ? GetObjectComponent<T>(obj) : null;
        }

        public T GetObject<T>(int id) where T : Object
        {
            var obj = GetObject(id);
            return obj != null ? GetObjectComponent<T>(obj) : null;
        }

        public object GetObject(int id, Type type)
        {
            var obj = GetObject(id);
            return obj != null ? GetObjectComponent(obj, type) : null;
        }

        public object GetObject(string id, Type type)
        {
            var obj = GetObject(id);
            return obj != null ? GetObjectComponent(obj, type) : null;
        }

        private T GetObjectComponent<T>(Object obj) where T : Object
        {
            if (obj is GameObject go)
                return go.GetComponent<T>();
            if (obj is T tObj)
                return tObj;
            return (obj as Component)?.GetComponent<T>();
        }
        
        private Component GetObjectComponent(Object obj, Type type)
        {
            if (obj is GameObject go)
                return go.GetComponent(type);
            return (obj as Component)?.GetComponent(type);
        }
    }
}