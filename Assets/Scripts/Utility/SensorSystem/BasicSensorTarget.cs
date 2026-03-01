using UnityEngine;

namespace Utility.SensorSystem
{
    public class BasicSensorTarget : MonoBehaviour, ISensorTarget
    {
        [SerializeField] private HashId _senseId;

        public HashId SenseId => _senseId;
        public bool IsColliderActive => MainCollider.enabled;
        public Collider MainCollider { get; private set; }
        
        private void Awake()
        {
            MainCollider = GetComponent<Collider>();
        }
    }
}