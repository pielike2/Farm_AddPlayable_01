using System.Collections;
using UnityEngine;

namespace Playable.Gameplay.Misc
{
    public class PhysicObjectsScattering : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] _objects;
        [SerializeField] private Vector3 _force = Vector3.forward;
        [SerializeField] private float _kinematicStateDelay = 2f;

        private void OnEnable()
        {
            for (int i = 0; i < _objects.Length; i++)
                _objects[i].AddForce(_force, ForceMode.Impulse);

            StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            yield return new WaitForSeconds(_kinematicStateDelay);

            for (var i = 0; i < _objects.Length; i++)
                _objects[i].isKinematic = true;
        }
        
        [ContextMenu("Cache")]
        private void Cache()
        {
            _objects = GetComponentsInChildren<Rigidbody>(true);
        }
    }
}