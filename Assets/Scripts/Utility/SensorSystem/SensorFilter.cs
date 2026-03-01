using System;
using System.Collections.Generic;

namespace Utility.SensorSystem
{
    public class SensorFilter : IDisposable
    {
        private readonly BaseSensor _sensor;
        private readonly HashId _sensorTargetId;

        private static readonly List<ISensorTarget> _targetsBufList = new List<ISensorTarget>();
        
        public bool IsBlocked { get; set; }
        
        public List<ISensorTarget> FilteredTargets { get; }
        public event Action<ISensorTarget> OnAddTarget;
        public event Action<ISensorTarget> OnRemoveTarget;

        public SensorFilter(BaseSensor sensor, HashId targetId)
        {
            FilteredTargets = new List<ISensorTarget>();
            
            _sensor = sensor;
            
            _sensor.OnAddTarget += OnSensorAddTarget;
            _sensor.OnRemoveTarget += OnSensorRemoveTarget;

            _sensorTargetId = targetId;

            foreach (var target in sensor.ActiveTargets)
                if (target.SenseId == _sensorTargetId)
                    AddFilteredTarget(target);
        }

        public void Dispose()
        {
            _sensor.OnAddTarget -= OnSensorRemoveTarget;
            _sensor.OnAddTarget -= OnSensorAddTarget;
            OnAddTarget = null;
            OnRemoveTarget = null;
        }

        private void AddFilteredTarget(ISensorTarget target)
        {
            if (IsBlocked)
                return;
            FilteredTargets.Add(target);
            OnAddTarget?.Invoke(target);
        }

        private void RemoveFilteredTarget(ISensorTarget target)
        {
            var removed = FilteredTargets.Remove(target);
            if (removed)
                OnRemoveTarget?.Invoke(target);
        }

        private void OnSensorAddTarget(ISensorTarget target)
        {
            if (target.SenseId == _sensorTargetId)
                AddFilteredTarget(target);
        }

        private void OnSensorRemoveTarget(ISensorTarget target)
        {
            RemoveFilteredTarget(target);
        }

        public void ForceRemoveFilteredTarget(ISensorTarget target)
        {
            RemoveFilteredTarget(target);
        }

        public void ForceAddFilteredTarget(ISensorTarget target)
        {
            AddFilteredTarget(target);
        }

        public void ForceRemoveFilteredTargets()
        {
            _targetsBufList.Clear();
            _targetsBufList.AddRange(FilteredTargets);
            foreach (var target in _targetsBufList)
                RemoveFilteredTarget(target);
        }
    }
}