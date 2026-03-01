using System;
using UnityEngine;

namespace Utility
{
    [Serializable]
    public class MarkedReference<T> : IDisposable where T : class
    {
        private int _hashedMarkId;
        private int _hashedNestedId;
        private T _cachedReference;
        private bool _isNull = true;
        private bool _isInitialized;

        public event Action<T, T> OnReferenceChanged;

        protected virtual string __markId { get; set; }
        protected virtual string __nestedId { get; set; }
        protected virtual bool __skipInactive { get; set; }

        public string MarkId 
        { 
            get => __markId;
            set
            {
                if (__markId == value)
                    return;
                Unsubscribe();
                __markId = value;
                _hashedMarkId = HashUtil.StringToHash(__markId);
                RefreshReference();
                Subscribe();
            }
        }

        public string NestedId
        {
            get => __nestedId;
            set
            {
                if (__nestedId == value)
                    return;
                __nestedId = value;
                _hashedNestedId = HashUtil.StringToHash(__nestedId);
                RefreshReference();
            }
        }

        public bool SkipInactive
        {
            get => __skipInactive;
            set
            {
                if (__skipInactive == value)
                    return;
                __skipInactive = value;
                RefreshReference();
            }
        }

        public int HashedMarkId 
        {
            get
            {
                TryInit();
                return _hashedMarkId;
            }
        }

        public bool IsNull
        {
            get
            {
                TryInit();
                return _isNull;
            }
        }

        public bool IsValid => !IsNull;

        public T Value
        {
            get
            {
                TryInit();
                return _cachedReference;
            }
        }

        public MarkedReference()
        {
        }

        public MarkedReference(string markId, string nestedId, bool skipInactive = false)
        {
            __markId = markId;
            __nestedId = nestedId;
            __skipInactive = skipInactive;
        }

        public MarkedReference(string markId, bool skipInactive = false) : this(markId, null, skipInactive)
        {
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        private void TryInit()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            
            _hashedMarkId = HashUtil.StringToHash(__markId);
            _hashedNestedId = HashUtil.StringToHash(__nestedId);
            
            RefreshReference();
            Subscribe();
        }

        private void Subscribe()
        {
            MarkedObject.OnMarkedObjectChanged += OnMarkedObjectChangedInternal;
        }

        private void Unsubscribe()
        {
            MarkedObject.OnMarkedObjectChanged -= OnMarkedObjectChangedInternal;
        }

        private void OnMarkedObjectChangedInternal(int changedId, MarkedObject oldObj, MarkedObject newObj)
        {
            if (changedId != _hashedMarkId) 
                return;

            var previousReference = _cachedReference;
            RefreshReference();

            if (previousReference != _cachedReference)
                OnReferenceChanged?.Invoke(previousReference, _cachedReference);
        }

        private void RefreshReference()
        {
            var markedObject = GetBestObject(_hashedMarkId);

            if (markedObject == null)
            {
                _cachedReference = null;
                _isNull = true;
                return;
            }

            // If nested id is specified, try to get the nested reference via ReferencesHolder
            if (_hashedNestedId != 0)
            {
                var holder = markedObject.GetComponent<ReferencesHolder>();
                if (holder == null)
                    return;
                var nestedObject = holder.GetObject(_hashedNestedId, typeof(T)) as T;
                _cachedReference = nestedObject;
                _isNull = _cachedReference == null;
                return;
            }

            // Get the component from the marked object directly
            _cachedReference = markedObject.GetComponent<T>();
            _isNull = _cachedReference == null;
        }

        private MarkedObject GetBestObject(int id)
        {
            if (!MarkedObject.TryGetAll(id, out var list))
                return null;

            if (__skipInactive)
            {
                // First active object
                foreach (var obj in list)
                    if (obj != null && obj.gameObject.activeInHierarchy)
                        return obj;
            }
            else
            {
                foreach (var obj in list)
                    if (obj != null)
                        return obj;
            }

            return null;
        }

        public void ForceRefresh()
        {
            var previousReference = _cachedReference;
            RefreshReference();

            if (previousReference != _cachedReference)
                OnReferenceChanged?.Invoke(previousReference, _cachedReference);
        }

        public override string ToString()
        {
            var nestedInfo = string.IsNullOrEmpty(__nestedId) ? "" : $".{__nestedId}";
            return $"MarkedObjectReference<{typeof(T).Name}>({__markId}{nestedInfo}): {(_isNull ? "null" : _cachedReference?.ToString())}";
        }
    }
}