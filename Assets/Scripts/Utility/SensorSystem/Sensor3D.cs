using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utility.SensorSystem
{
    public class Sensor3D : BaseSensor
    {
        private Sensor3D_TriggerStay _triggerStay;
        private int _triggerStayCheckCounter;

        private void Awake()
        {
            _triggerStay = GetComponent<Sensor3D_TriggerStay>();
            if (_triggerStay)
                _triggerStay.OnAddTarget += AddTarget;
        }

        private void OnTriggerEnter(Collider col)
        {
            var targets = col.GetComponents<ISensorTarget>();
            for (int i = 0; i < targets.Length; i++)
                AddTarget(targets[i]);
        }

        private void OnTriggerExit(Collider col)
        {
            var targets = col.GetComponents<ISensorTarget>();
            for (int i = 0; i < targets.Length; i++)
                RemoveTarget(targets[i]);
        }

        private void Update()
        {
            foreach (var target in ActiveTargets)
                if (!target.gameObject.activeInHierarchy || !target.IsColliderActive)
                    _targetsBuf.Add(target);
            
            if (_targetsBuf.Count > 0)
            {
                foreach (var target in _targetsBuf)
                    RemoveTarget(target);
                _targetsBuf.Clear();
            }
        }
    }
}