using System.Collections.Generic;
using UnityEngine;
using Playable.Gameplay.Placements;
using Playable.Gameplay.Character;

namespace Playable.Gameplay.Effects
{
    public class CargoOutlineController : MonoBehaviour
    {
        [SerializeField] private GameObject _outlinedModelPrefab;
        [SerializeField] private bool _highlightOnStart = false;
        [SerializeField] private Placement _placementDeactivate;

        private BaseCargo _cargo;

        private class OutlineData
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        private class SavedState
        {
            public Renderer Renderer;
            public MeshFilter MeshFilter;
            public Material[] Materials;
            public Mesh Mesh;
        }

        private OutlineData _outline;
        private readonly Dictionary<Transform, SavedState> _saved = new Dictionary<Transform, SavedState>();

        private void Awake()
        {
            _cargo = GetComponent<BaseCargo>();
            if (_cargo != null)
            {
                _cargo.OnReleaseFromSlot += OnItemReleased;
                _cargo.OnOccupySlot += OnItemOccupied;
            }

            if (_placementDeactivate != null)
            {
                _placementDeactivate.OnStartInteraction += OnPlacementEnter;
                _placementDeactivate.OnStopInteraction += OnPlacementExit;
            }
        }

        private void Start()
        {
            if (_highlightOnStart)
                HighlightAllItems();
        }

        private void OnDestroy()
        {
            RemoveAllHighlights();

            if (_cargo != null)
            {
                _cargo.OnReleaseFromSlot -= OnItemReleased;
                _cargo.OnOccupySlot -= OnItemOccupied;
            }

            if (_placementDeactivate != null)
            {
                _placementDeactivate.OnStartInteraction -= OnPlacementEnter;
                _placementDeactivate.OnStopInteraction -= OnPlacementExit;
            }
        }

        public bool IsItemOutlined(Transform item) => item != null && _saved.ContainsKey(item);

        public void HighlightAllItems()
        {
            if (_cargo == null) return;
            for (int i = 0; i < _cargo.Items.Count; i++)
                ApplyOutlineToItem(_cargo.Items[i]);
        }

        public void RemoveAllHighlights()
        {
            if (_cargo == null) return;
            for (int i = _cargo.Items.Count - 1; i >= 0; i--)
                RemoveOutlineFromItem(_cargo.Items[i]);
        }

        private void OnItemReleased(Transform slot, Transform item)
        {
            RemoveOutlineFromItem(item);
        }

        private void OnItemOccupied(Transform slot, Transform item)
        {
            if (_highlightOnStart)
                ApplyOutlineToItem(item);
        }

        private void OnPlacementEnter(Placement placement, IInteractor interactor)
        {
            RemoveAllHighlights();
        }

        private void OnPlacementExit(Placement placement, IInteractor interactor)
        {
            HighlightAllItems();
        }

        private void EnsureOutline()
        {
            if (_outline != null) return;
            if (_outlinedModelPrefab == null) return;

            var srcRenderer = _outlinedModelPrefab.GetComponent<Renderer>();
            if (srcRenderer == null) return;

            var srcMF = _outlinedModelPrefab.GetComponent<MeshFilter>();

            _outline = new OutlineData
            {
                Materials = srcRenderer.sharedMaterials,
                Mesh = srcMF != null ? srcMF.sharedMesh : null
            };
        }

        private void ApplyOutlineToItem(Transform item)
        {
            if (item == null) return;
            if (_saved.ContainsKey(item)) return;

            EnsureOutline();
            if (_outline == null) return;

            var r = item.GetComponent<Renderer>();
            if (r == null) return;

            var mf = item.GetComponent<MeshFilter>();

            var state = new SavedState
            {
                Renderer = r,
                MeshFilter = mf,
                Materials = r.sharedMaterials,
                Mesh = mf != null ? mf.sharedMesh : null
            };

            r.sharedMaterials = _outline.Materials;
            if (mf != null) mf.sharedMesh = _outline.Mesh;

            _saved[item] = state;
        }

        private void RemoveOutlineFromItem(Transform item)
        {
            if (item == null) return;
            if (!_saved.TryGetValue(item, out var s)) return;

            if (s.Renderer != null)
                s.Renderer.sharedMaterials = s.Materials;

            if (s.MeshFilter != null)
                s.MeshFilter.sharedMesh = s.Mesh;

            _saved.Remove(item);
        }
    }
}