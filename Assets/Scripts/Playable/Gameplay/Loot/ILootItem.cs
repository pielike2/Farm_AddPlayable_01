using System;
using Base.PoolingSystem;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Loot
{
    public interface ILootItem : IMonoPoolObject
    {
        HashId ItemTypeId { get; }
        bool IsCurrencyItem { get; }
        bool IsUsable { get; }
        bool WasPickedUpOnce { get; }
        ILootItem NextSourcePrefab { get; set; }
        bool HasRigidbody { get; }
        Rigidbody Rigidbody { get; }

        bool TryPickup();
        void ToggleRigidbody(bool value);
        void CopyFromSourcePrefab(ILootItem other);
    }
}