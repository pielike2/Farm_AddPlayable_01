using System;
using System.Collections.Generic;
using Base;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utility;
using Utility.Extensions;

namespace Playable.Gameplay
{
    public class Cargo : BaseCargo
    {
        [SerializeField, HideInInspector] private List<Transform> _slots = new List<Transform>();
        [SerializeField] private Vector3Int _size = new Vector3Int(1, 100, 1);
        [SerializeField] private Vector3 _interval = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] private Vector3 _slotRotation;

        [Space(10)] 
        [SerializeField] private HashId _itemTypeId;
        
        [Space(10)]
        [SerializeField] private bool _fallAnimationEnabled;
        [SerializeField] private float _itemFallDuration = 0.2f;
        [SerializeField] private float _itemFallDelay = 0.1f;
        
        [SerializeField] private float _additionalPreInterval;
        [SerializeField] private float _additionalPostInterval;
        
#if UNITY_EDITOR
        [Header("For Editor")]
        [Space(10)]
        [SerializeField] private GameObject _previewPrefab;
        [SerializeField] private bool _fillPreviewObjects = true;
        [SerializeField] private Vector3 _previewObjectRotation;
        [SerializeField] private float _previewObjectScale = 1f;
        [SerializeField] private bool _gizmosEnabled = true;
        [SerializeField] private Color _slotGizmoColor = Color.yellow.SetAlpha(0.4f);
        [SerializeField] private float _slotGizmoSize = 0.07f;
#endif

        private readonly Dictionary<Transform, Transform> _slotItems = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Transform, Transform> _itemSlots = new Dictionary<Transform, Transform>();
        private readonly List<Transform> _transformBuf = new List<Transform>();
        private int _occupiedSlotsCount;
        private int _bottomReleaseCounter;
        private Tween[] _fallAnimTweens;
        private int _activeFallTweensCount;

        public override List<Transform> Slots
        {
            get => _slots;
            protected set => _slots = value;
        }

        public override int OccupiedSlotsCount
        {
            get => _occupiedSlotsCount;
            protected set => _occupiedSlotsCount = value;
        }

        public override Vector3 Interval
        {
            get => _interval;
            protected set => _interval = value;
        }

        public override Vector3 SlotRotation
        {
            get => _slotRotation;
            protected set => _slotRotation = value;
        }

        public override Vector3Int Size => _size;

        public override HashId ItemTypeId
        {
            get => _itemTypeId;
            protected set => _itemTypeId = value;
        }
        
        public override float AdditionalPreInterval => _additionalPreInterval;
        public override float AdditionalPostInterval => _additionalPostInterval;

        public override event Action<Transform, Transform> OnOccupySlot; 
        public override event Action<Transform, Transform> OnReleaseFromSlot;
        
        #region Editor
#if UNITY_EDITOR
        public override GameObject PreviewPrefab => _previewPrefab;

        private void OnValidate()
        {
            if (_size.x < 1)
                _size.x = 1;
            if (_size.y < 1)
                _size.y = 1;
            if (_size.z < 1)
                _size.z = 1;
            
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.delayCall += RecacheCargo;
        }

        private void OnDrawGizmos()
        {
            if (!_gizmosEnabled)
                return;
            
            foreach (var slot in _slots)
            {
                if (!slot)
                    continue;
                Gizmos.color = _slotGizmoColor;
                Gizmos.DrawSphere(slot.position, _slotGizmoSize);
            }

            if (!Application.isPlaying)
                return;
            
            foreach (var item in Items)
            {
                var slotFound = TryGetItemSlot(item, out var slot);
                Gizmos.color = slotFound ? Color.blue : Color.red; 
                Gizmos.DrawSphere(item.position, _slotGizmoSize);
                if (slotFound)
                    Gizmos.DrawLine(item.position, slot.position);
            }
        }

        [ContextMenu("Test Spawn Window")]
        private void OpenTestSpawnWindow()
        {
            InvokeOpenTestSpawnWindow();
        }

        private bool SlotsAreValid()
        {
            var expectedCount = _size.x * _size.y * _size.z;
            if (_slots.Count != expectedCount)
                return false;

            var expectedRotation = Quaternion.Euler(_slotRotation);
            var currentSlotIndex = 0;

            for (var y = 0; y < _size.y; y++)
            for (var x = 0; x < _size.x; x++)
            for (var z = 0; z < _size.z; z++)
            {
                var currentSlot = _slots[currentSlotIndex++];
                if (currentSlot == null)
                    return false;

                var expectedPosition = new Vector3(x * _interval.x, y * _interval.y, z * _interval.z);
                var isPositionValid = (currentSlot.localPosition - expectedPosition).sqrMagnitude < 0.0001f;
                var isRotationValid = Quaternion.Angle(currentSlot.localRotation, expectedRotation) < 0.1f;

                if (!isPositionValid || !isRotationValid)
                    return false;
            }

            return true;
        }

        private void RecacheCargo()
        {
            if (this == null || PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;
            if (SlotsAreValid()) 
                return;
            
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("slot"))
                    DestroyImmediate(child.gameObject);
            }
            
            _slots.Clear();

            for (int k = 0; k < _size.y; k++)
            for (int i = 0; i < _size.x; i++)
            for (int j = 0; j < _size.z; j++)
                CreateSlot(i, j, k);

            // _slots = _slots.OrderBy(slot => slot.position.y).ToList();
            EditorUtility.SetDirty(this);
        }

        private void CreateSlot(int i, int j, int k)
        {
            var newSlot = new GameObject($"slot_{_slots.Count}").transform;
            newSlot.SetParent(transform, false);
            newSlot.localPosition = new Vector3(i * _interval.x, k * _interval.y, j * _interval.z);
            newSlot.localRotation = Quaternion.Euler(SlotRotation);
            _slots.Add(newSlot);

            if (_fillPreviewObjects && PreviewPrefab)
            {
                var previewObj = Instantiate(PreviewPrefab, newSlot, false);
                previewObj.transform.localPosition = Vector3.zero;
                previewObj.transform.localRotation = Quaternion.Euler(_previewObjectRotation);
                previewObj.transform.localScale *= _previewObjectScale;
                previewObj.hideFlags = HideFlags.HideAndDontSave;
            }
        }
#endif
        #endregion

        private void Awake()
        {
            Items = new List<Transform>();
            LayerSize = _size.x * _size.z;
            
            if (_fallAnimationEnabled)
                _fallAnimTweens = new Tween[Slots.Count];
        }

        public override void ClearEvents()
        {
            OnOccupySlot = null;
            OnReleaseFromSlot = null;
        }

        public override Transform OccupyNextSlot(Transform item)
        {
            if (_occupiedSlotsCount >= _slots.Count) 
                return _slots[_slots.Count - 1];

            Transform slot = null;
            for (int i = _occupiedSlotsCount; i < _slots.Count; i++)
            {
                if (IsSlotBusy(_slots[i])) 
                    continue;
                slot = _slots[i];
                break;
            }

            OccupySlot(item, slot);
            return slot;
        }

        public override void OccupySlot(Transform item, Transform slot, bool fireEvent = true)
        {
            _slotItems[slot] = item;
            _itemSlots[item] = slot;
            Items.Add(item);
            _occupiedSlotsCount++;

            if (_occupiedSlotsCount > _slots.Count)
                _occupiedSlotsCount = _slots.Count;

            PostprocessSlots();
            
            if (fireEvent)
                OnOccupySlot?.Invoke(slot, item);
        }

        public override void ReplaceItemInSlot(Transform slot, Transform oldItem, Transform newItem)
        {
            _slotItems[slot] = newItem;
            _itemSlots[newItem] = slot;
            Items.Remove(oldItem);
            Items.Add(newItem);
        }

        public override void ReleaseItemFromSlot(Transform item, bool fireEvent = true)
        {
            if (!_itemSlots.TryGetValue(item, out var slot))
                return;
            _slotItems.Remove(slot);
            _itemSlots.Remove(item);
            _occupiedSlotsCount--;
            Items.Remove(item);
            
            PostprocessSlots();
            
            if (fireEvent)
                OnReleaseFromSlot?.Invoke(slot, item);
        }

        public override void FreeAllSlots()
        {
            _transformBuf.AddRange(Items);
            foreach (var item in _transformBuf)
                ReleaseItemFromSlot(item);
        }

        private void PostprocessSlots()
        {
            HighestItemPos = _occupiedSlotsCount > 0
                ? _slots[Math.Min(_slots.Count, _occupiedSlotsCount) - 1].position
                : _slots[0].position;
        }

        public override bool IsSlotBusy(Transform slot)
        {
            return _slotItems.ContainsKey(slot);
        }

        public override bool TryGetSlotItem(Transform slot, out Transform item)
        {
            return _slotItems.TryGetValue(slot, out item);
        }

        public override bool TryGetItemSlot(Transform item, out Transform slot)
        {
            return _itemSlots.TryGetValue(item, out slot);
        }

        public override bool TryReleaseTopItem(out Transform releasedItem, out Transform sourceSlot)
        {
            releasedItem = null;
            sourceSlot = null;
            
            if (_occupiedSlotsCount == 0)
                return false;
            
            var topSlot = _slots[_occupiedSlotsCount - 1];
            if (!_slotItems.TryGetValue(topSlot, out var item)) 
                return false;
            
            ReleaseItemFromSlot(item);
            releasedItem = item;
            sourceSlot = topSlot;
            
            return true;
        }

        public override bool TryReleaseBottomItem(out Transform releasedItem, bool calculateFall = true)
        {
            releasedItem = null;
            if (_occupiedSlotsCount == 0)
                return false;
            
            Transform item = null;
            var bottomItemFound = false;
            var startInd = _bottomReleaseCounter % LayerSize; 
            for (int i = startInd; i < startInd + LayerSize; i++)
            {
                if (_slotItems.TryGetValue(_slots[i % LayerSize], out item))
                {
                    bottomItemFound = true;
                    break;
                }
            }
            if (!bottomItemFound)
                return false;

            _bottomReleaseCounter++;
            ReleaseItemFromSlot(item);
            releasedItem = item;

            if (calculateFall)
                CalculateFall();
            
            return true;
        }

        public override void CalculateFall()
        {
            var n = _occupiedSlotsCount + LayerSize;
            for (int i = 1; i < n; i++)
            {
                if (i >= _slots.Count)
                    break;
                var currentSlot = _slots[i];
                var lowerSlotInd = i - LayerSize;
                if (lowerSlotInd < 0)
                    continue;
                var lowerSlot = _slots[lowerSlotInd];
                if (!_slotItems.TryGetValue(currentSlot, out var currentItem) || IsSlotBusy(lowerSlot))
                    continue;
                ReleaseItemFromSlot(currentItem, false);
                OccupySlot(currentItem, lowerSlot, false);
            }
        }
        
        public override void AnimateFall()
        {
            for (int i = 0; i < _activeFallTweensCount; i++)
                _fallAnimTweens[i].Kill();

            _activeFallTweensCount = 0;

            Transform item;
            Vector3 dt;
            for (var i = 0; i < Items.Count; i++)
            {
                item = Items[i];
                if (!TryGetItemSlot(item, out var slot))
                    continue;
                
                item.SetParent(slot);
                dt = slot.localPosition - (item.parent.localPosition + item.localPosition);
                var fallTween = item.DOBlendableLocalMoveBy(dt, _itemFallDuration).SetEase(Ease.Linear);
                if (_itemFallDelay > 0f)
                    fallTween.SetDelay(_itemFallDelay);

#if UNITY_EDITOR
                if (_fallAnimTweens == null && !_fallAnimationEnabled)
                {
                    Debug.LogError($"Cargo cannot animate fall (it needs to be enabled).{Environment.NewLine}Cargo path: '{transform.GetHierarchyPath()}'{Environment.NewLine}");
                    return;
                }
#endif
                _fallAnimTweens[_activeFallTweensCount] = fallTween;
                _activeFallTweensCount++;
            }
        }

        public override Transform PutTransformIntoSlot(Transform item, Transform slot, bool skipExtraCalls = false)
        {
            item.SetParent(slot);
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
            return item;
        }
    }
}
