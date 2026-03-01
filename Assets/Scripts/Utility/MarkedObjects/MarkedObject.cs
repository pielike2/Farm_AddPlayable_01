using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    [ExecuteAlways]
    public class MarkedObject : MonoBehaviour
    {
        [SerializeField] private string _markId = "Undefined";

        private static Dictionary<int, List<MarkedObject>> _markedObjects = new Dictionary<int, List<MarkedObject>>();

        public static event Action<int, MarkedObject, MarkedObject> OnMarkedObjectChanged;

        public string MarkId => _markId;
        public int HashedMarkId { get; private set; }

        private void Awake()
        {
            HashedMarkId = HashUtil.StringToHash(_markId);
            RegisterObject();
        }

        private void OnEnable()
        {
            NotifyReferenceChange();
        }

        private void OnDisable()
        {
            NotifyReferenceChange();
        }

        private void OnDestroy()
        {
            UnregisterObject();
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            
            var oldHash = HashedMarkId;
            HashedMarkId = HashUtil.StringToHash(_markId);
            
            if (oldHash != HashedMarkId)
            {
                UnregisterObject();
                RegisterObject();
            }
        }

        private void RegisterObject()
        {
            if (!_markedObjects.ContainsKey(HashedMarkId))
                _markedObjects[HashedMarkId] = new List<MarkedObject>();

            var list = _markedObjects[HashedMarkId];
            if (!list.Contains(this))
            {
                list.Add(this);
                NotifyReferenceChange();
            }
        }

        private void UnregisterObject()
        {
            if (_markedObjects.ContainsKey(HashedMarkId))
            {
                var list = _markedObjects[HashedMarkId];
                list.Remove(this);

                if (list.Count == 0)
                    _markedObjects.Remove(HashedMarkId);

                NotifyReferenceChange();
            }
        }

        private void NotifyReferenceChange()
        {
            var currentBest = GetInternal(HashedMarkId);
            OnMarkedObjectChanged?.Invoke(HashedMarkId, null, currentBest);
        }

        private static MarkedObject GetInternal(int id)
        {
            if (!_markedObjects.TryGetValue(id, out var list))
                return null;

            foreach (var obj in list)
                if (obj != null)
                    return obj;

            return null;
        }

        public static MarkedObject Get(int id)
        {
            return GetInternal(id);
        }

        public static MarkedObject Get(string id)
        {
            return GetInternal(HashUtil.StringToHash(id));
        }

        public static bool TryGetAll(int id, out List<MarkedObject> list)
        {
            return _markedObjects.TryGetValue(id, out list);
        }

        public static bool TryGetAll(string id, out List<MarkedObject> list)
        {
            return _markedObjects.TryGetValue(HashUtil.StringToHash(id), out list);
        }
    }
}