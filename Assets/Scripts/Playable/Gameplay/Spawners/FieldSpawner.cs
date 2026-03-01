using System;
using System.Collections;
using Playable.Gameplay.Collectables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utility.Extensions;

namespace Playable.Gameplay.Spawners
{
    public class FieldSpawner : MonoBehaviour
    {
        [SerializeField] private BaseCollectable _collectablePrefab;
        [SerializeField] private Vector2 _spaceInterval = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2Int _fieldSize = new Vector2Int(10, 10);
        [SerializeField] private Vector3 _defaultRotation;
        [SerializeField] private bool _randomizeRotation;
        [SerializeField] private int _randomSeed;

        [SerializeField] private float _startSpawnDelay = 0f;
        [SerializeField] private float _startSpawnInterval = 0f;
        [SerializeField] private bool _flipVertical;
        
#if UNITY_EDITOR
        [Header("For Editor")]
        [SerializeField] private bool _previewEnabled;
        [SerializeField] private bool _gizmosEnabled = true;
        [SerializeField] private Color _slotGizmoColor = Color.green.SetAlpha(0.8f);
        [SerializeField] private float _slotGizmoSize = 0.07f;
#endif

        private System.Random _random;
        private WaitForSeconds _interval;

        private IEnumerator Start()
        {
            _random = new System.Random(_randomSeed);

            if (_startSpawnDelay > 0)
                yield return new WaitForSeconds(_startSpawnDelay);

            if (_startSpawnInterval > 0)
            {
                _interval = new WaitForSeconds(_startSpawnInterval);
                
                StartCoroutine(ProcessFieldRoutine((pos, r) =>
                {
                    var instance = Instantiate(_collectablePrefab, pos, Quaternion.identity, transform);
                    instance.transform.localRotation = r;
                }));
                
                yield break;
            }

            ProcessField((pos, r) =>
            {
                var instance = Instantiate(_collectablePrefab, pos, Quaternion.identity, transform);
                instance.transform.localRotation = r;
            });
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_gizmosEnabled)
                return;
            Gizmos.color = _slotGizmoColor;
            ProcessField((pos, r) =>
            {
                Gizmos.DrawSphere(pos, _slotGizmoSize);
            });
        }

        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return; // Защита от вызова на удаленном объекте
                ClearChildren();
                if (_previewEnabled)
                    CreatePreview();
            };
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }

        private void CreatePreview()
        {
            ClearChildren();
    
            ProcessField((pos, r) =>
            {
                var instance = Instantiate(_collectablePrefab, pos, r, transform);
                instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            });
        }
#endif

        private void ProcessField(Action<Vector3, Quaternion> action)
        {
            _random ??= new System.Random(_randomSeed);
            
            for (int i = 0; i < _fieldSize.x; i++)
            {
                for (int j = 0; j < _fieldSize.y; j++)
                {
                    var localOffset = new Vector3(i * _spaceInterval.x, 0, j * _spaceInterval.y);
                    var pos = transform.TransformPoint(localOffset);
                    var r = _randomizeRotation
                        ? Quaternion.Euler(0f, (float)_random.NextDouble() * 360f, 0f)
                        : Quaternion.Euler(_defaultRotation);
                    action?.Invoke(pos, r);
                }
            }
        }
        
        private IEnumerator ProcessFieldRoutine(Action<Vector3, Quaternion> action)
        {
            _random ??= new System.Random(_randomSeed);

            if (!_flipVertical)
            {
                for (int i = 0; i < _fieldSize.x; i++)
                {
                    for (int j = 0; j < _fieldSize.y; j++)
                    {
                        var localOffset = new Vector3(i * _spaceInterval.x, 0, j * _spaceInterval.y);
                        var pos = transform.TransformPoint(localOffset);
                        var r = _randomizeRotation
                            ? Quaternion.Euler(0f, (float)_random.NextDouble() * 360f, 0f)
                            : Quaternion.Euler(_defaultRotation);
                        action?.Invoke(pos, r);

                        yield return _interval;
                    }
                }

                yield break;
            }

            for (int i = 0; i < _fieldSize.y; i++)
            {
                for (int j = 0; j < _fieldSize.x; j++)
                {
                    var localOffset = new Vector3(j * _spaceInterval.x, 0, i * _spaceInterval.y);
                    var pos = transform.TransformPoint(localOffset);
                    var r = _randomizeRotation
                        ? Quaternion.Euler(0f, (float)_random.NextDouble() * 360f, 0f)
                        : Quaternion.Euler(_defaultRotation);
                    action?.Invoke(pos, r);

                    yield return _interval;
                }
            }
        }
    }
}
