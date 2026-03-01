using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.SensorSystem
{
    public class BaseSensor : MonoBehaviour
    {
        private readonly Dictionary<int, SensorFilter> _filters = new Dictionary<int, SensorFilter>();
        protected readonly List<ISensorTarget> _targetsBuf = new List<ISensorTarget>();
        
        public HashSet<ISensorTarget> ActiveTargets { get; private set; } = new HashSet<ISensorTarget>();

        public event Action<ISensorTarget> OnAddTarget;
        public event Action<ISensorTarget> OnRemoveTarget;

        protected virtual void OnDisable()
        {
            RemoveAllTargets();
        }

        protected virtual void OnDestroy()
        {
            foreach (var filter in _filters)
                filter.Value.Dispose();
            _filters.Clear();
        }

        protected void AddTarget(ISensorTarget target)
        {
            if (ActiveTargets.Add(target))
                OnAddTarget?.Invoke(target);
        }

        protected void RemoveTarget(ISensorTarget target)
        {
            var removed = ActiveTargets.Remove(target);
            if (removed)
                OnRemoveTarget?.Invoke(target);
        }

        protected void RemoveAllTargets()
        {
            _targetsBuf.AddRange(ActiveTargets);
            foreach (var target in _targetsBuf)
                RemoveTarget(target);
            _targetsBuf.Clear();
        }

        public SensorFilter GetFilter(HashId targetId)
        {
            if (_filters.TryGetValue(targetId.Hash, out var filter))
                return filter;
            var newFilter = new SensorFilter(this, targetId);
            _filters[targetId.Hash] = newFilter;
            return newFilter;
        }

        public void ForceRemoveFilterTargets(HashId senseId)
        {
            var filter = GetFilter(senseId);
            filter.ForceRemoveFilteredTargets();
        }

        public void BlockFilteredTargets(HashId senseId)
        {
            var filter = GetFilter(senseId);
            filter.IsBlocked = true;
        }

        public void UnblockFilteredTargets(HashId senseId)
        {
            var filter = GetFilter(senseId);
            filter.IsBlocked = false;
        }

        public void BlockFilteredTargets(string senseId)
        {
            var filter = GetFilter(new HashId(senseId));
            filter.IsBlocked = true;
        }

        public void UnblockFilteredTargets(string senseId)
        {
            var filter = GetFilter(new HashId(senseId));
            filter.IsBlocked = false;
        }
    }
}