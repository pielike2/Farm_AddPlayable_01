using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Playable.Gameplay
{
    public class BaseCargo : MonoBehaviour
    {
        public virtual List<Transform> Slots { get; protected set; }
        public virtual List<Transform> Items { get; protected set; }
        public virtual int OccupiedSlotsCount { get; protected set; }
        public virtual bool IsFull => OccupiedSlotsCount >= Slots.Count;
        public virtual int LayerSize { get; protected set; }
        public virtual Vector3 SlotRotation { get; protected set; }
        public virtual Vector3 Interval { get; protected set; }
        public virtual Vector3Int Size { get; protected set; }
        public virtual Vector3 HighestItemPos { get; protected set; }
        public virtual HashId ItemTypeId { get; protected set; }

        public virtual event Action<Transform, Transform> OnOccupySlot;
        public virtual event Action<Transform, Transform> OnReleaseFromSlot;

        public virtual void ClearEvents() { }
        public virtual Transform OccupyNextSlot(Transform item) { return null; }
        
        public virtual void OccupySlot(Transform item, Transform slot, bool fireEvent = true) { }
        public virtual void ReplaceItemInSlot(Transform slot, Transform oldItem, Transform newItem) { }
        public virtual void ReleaseItemFromSlot(Transform item, bool fireEvent = true) { }
        public virtual void FreeAllSlots() { }
        public virtual bool IsSlotBusy(Transform slot) { return false; }
        public virtual bool TryGetSlotItem(Transform slot, out Transform item) { item = null; return false; }
        public virtual bool TryGetItemSlot(Transform item, out Transform slot) { slot = null; return false; }
        public virtual bool TryReleaseTopItem(out Transform releasedItem, out Transform sourceSlot) { releasedItem = null; sourceSlot = null; return false; }
        public virtual bool TryReleaseBottomItem(out Transform releasedItem, bool calculateFall = true) { releasedItem = null; return false; }
        public virtual void CalculateFall() { }
        public virtual void AnimateFall() { }
        public virtual Transform PutTransformIntoSlot(Transform item, Transform slot, bool skipExtraCalls = false) { return item; }

        public virtual float AdditionalPreInterval { get; }
        public virtual float AdditionalPostInterval { get; }

#if UNITY_EDITOR
        public virtual GameObject PreviewPrefab { get; protected set; }
        public static event Action<BaseCargo> OnOpenTestSpawnWindow;
        protected void InvokeOpenTestSpawnWindow()
        {
            OnOpenTestSpawnWindow?.Invoke(this);
        }
#endif
    }
}