using System;
using UnityEngine;

namespace Utility.SensorSystem
{
    public class Sensor3D_TriggerStay : MonoBehaviour
    {
        [SerializeField] private int _triggerStayCheckInterval = 3;
        
        private int _triggerStayCheckCounter;
        
        public event Action<ISensorTarget> OnAddTarget;

        private void OnValidate()
        {
            if (_triggerStayCheckInterval < 1)
                _triggerStayCheckInterval = 1;
        }
        
        private void OnTriggerStay(Collider col)
        {
            if (_triggerStayCheckCounter++ % _triggerStayCheckInterval != 0)
                return;
            
            var targets = col.GetComponents<ISensorTarget>();
            for (int i = 0; i < targets.Length; i++)
                OnAddTarget?.Invoke(targets[i]);
        }
    }
}