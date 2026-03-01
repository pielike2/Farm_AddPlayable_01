using Base;
using Playable.Services;
using UnityEngine;
using Utility.SensorSystem;

namespace Utility
{
    public class AreaTicker : MonoBehaviour
    {
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] private HashId _targetSenseId;

        private TickByDemandService _service;
        private SensorFilter _sensorFilter;

        private void Awake()
        {
            _service = Get.Service<TickByDemandService>();
            _sensorFilter = _sensor.GetFilter(_targetSenseId);
            _sensorFilter.OnAddTarget += OnAddTarget;
            _sensorFilter.OnRemoveTarget += OnRemoveTarget;
        }

        private void OnAddTarget(ISensorTarget target)
        {
            if (target is ITickByDemand tickItem) 
                _service.AddTickTarget(tickItem);
        }

        private void OnRemoveTarget(ISensorTarget target)
        {
            if (target is ITickByDemand tickItem) 
                _service.RemoveTickTarget(tickItem);
        }
    }
}