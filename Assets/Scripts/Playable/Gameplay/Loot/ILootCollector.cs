using Playable.Gameplay.Collectables;
using UnityEngine;

namespace Playable.Gameplay.Loot
{
    public interface ILootCollector
    {
        bool CanCollect { get; }
        int LootCollectPointsCount { get; }
        int OverrideCollectCount { get; }
        Transform GetLootCollectPoint(int index);
        void CollectLoot(ILootItem lootItem, ICollectable collectable, int index);
    }
}