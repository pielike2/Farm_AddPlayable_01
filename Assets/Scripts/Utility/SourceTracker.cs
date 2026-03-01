using System;
using System.Collections.Generic;

namespace Utility
{
    public class SourceTracker
    {
        private readonly HashSet<object> _sources = new HashSet<object>();
        
        public bool IsActive => _sources.Count > 0;

        public event Action OnActivated;
        public event Action OnDeactivated;

        public void AddSource(object source)
        {
            if (_sources.Add(source) && _sources.Count == 1)
                OnActivated?.Invoke();
        }
        
        public bool RemoveSource(object source)
        {
            var removed = _sources.Remove(source);
            if (removed && _sources.Count == 0)
                OnDeactivated?.Invoke();
            return removed;
        }
        
        public void Clear()
        {
            var wasAny = _sources.Count > 0;
            _sources.Clear();
            if (wasAny)
                OnDeactivated?.Invoke();
        }
        
        public bool ContainsSource(object source)
        {
            return _sources.Contains(source);
        }

        public HashSet<object> GetSources()
        {
            return _sources;
        }
    }
}