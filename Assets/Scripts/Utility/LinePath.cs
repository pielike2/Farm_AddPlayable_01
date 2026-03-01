#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utility.Extensions;

namespace Utility
{
    [ExecuteAlways]
    public class LinePath : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField, HideInInspector] private Transform[] _points = new Transform[0];
        [SerializeField, HideInInspector] private float[] _distances = new float[0];
        [SerializeField, HideInInspector] private Vector3[] _directions = new Vector3[0];
#if UNITY_EDITOR
        [SerializeField] private Color _gizmosColor = Color.blue.SetAlpha(0.5f);
#endif

        private int _rootChildCount;

        public int PointsCount => _points.Length;
        public Transform[] Points => _points;

        private void Reset()
        {
            _root = transform;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            RecachePath();
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (_root.childCount != _rootChildCount)
            {
                _rootChildCount = _root.childCount;
                RecachePath();
            }
            if (Selection.activeTransform?.parent == _root)
                RecachePath();
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmosColor;
            for (int i = 0; i < Points.Length; i++)
            {
                if (Points[i] == null)
                    continue;
                Gizmos.DrawWireSphere(Points[i].position, 0.05f);
                if (i > 0)
                    Gizmos.DrawLine(Points[i - 1].position, Points[i].position);
            }
        }
#endif

        private void RecachePath()
        {
            _points = new Transform[_root.childCount];
            _distances = new float[_root.childCount];
            _directions = new Vector3[_root.childCount];
            
            var index = 0;
            foreach (Transform t in _root)
                Points[index++] = t;

            if (Points.Length == 0)
                return;

            for (int i = 0; i < Points.Length - 1; i++)
            {
                var dt = Points[i + 1].position - Points[i].position;
                _distances[i] = dt.magnitude;
                if (i > 0)
                    _distances[i] += _distances[i - 1];
                _directions[i] = dt.normalized;

                Points[i].name = $"point_{i}";
            }
            
            if (_directions.Length < 2)
                return;

            _distances[Points.Length - 1] = _distances[Points.Length - 2];
            _directions[Points.Length - 1] = _directions[Points.Length - 2];
            _directions[_directions.Length - 1] = _directions[_directions.Length - 2];
        }

        public bool TryGetPoint(int index, out Vector3 pos, out float dist, out Vector3 nextDir)
        {
            pos = Vector3.zero;
            nextDir = Vector3.zero;
            dist = 0f;
            if (index < 0 || index >= Points.Length)
                return false;
            pos = Points[index].position;
            dist = index > 0 ? _distances[index - 1] : 0f;
            nextDir = _directions[index];
            return true;
        }

        public int TransformToPointIndex(Transform t)
        {
            for (var i = 0; i < Points.Length; i++)
                if (Points[i] == t)
                    return i;
            return -1;
        }
        
        public void Sample(float distance, out Vector3 pos, out Vector3 dir, out int pointIndex)
        {
            pointIndex = 0;
            if (Points.Length < 2)
            {
                pos = Points[0].position;
                dir = Vector3.right;
                return;
            }

            if (distance > _distances[Points.Length - 1])
            {
                pointIndex = Points.Length - 1;
            }
            else
            {
                for (int i = 0; i < Points.Length; i++)
                {
                    if (_distances[i] < distance)
                        continue;
                    pointIndex = i;
                    break;
                }
            }

            dir = _directions[pointIndex];
            var prevDist = pointIndex < 1 ? 0f : _distances[pointIndex - 1];
            var localDist = distance - prevDist;
            pos = Points[pointIndex].position + dir * localDist;
        }
    }
}