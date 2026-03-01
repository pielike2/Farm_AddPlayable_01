using System;
using UnityEngine;

namespace Utility.Physics
{
    public class FixedUpdateColliderAttachment : MonoBehaviour
    {
        [SerializeField] private Transform _colliderRoot;

        private void Start()
        {
            _colliderRoot.SetParent(null, true);
        }

        private void OnDestroy()
        {
            if (_colliderRoot)
                Destroy(_colliderRoot.gameObject);
        }

        private void OnEnable()
        {
            _colliderRoot.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_colliderRoot)
                _colliderRoot.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            _colliderRoot.position = transform.position;
        }
    }
}