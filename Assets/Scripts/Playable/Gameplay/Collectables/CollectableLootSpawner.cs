using System.Collections;
using Base.PoolingSystem;
using Playable.Gameplay.Character;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Gameplay.Collectables
{
    public class CollectableLootSpawner : BaseCollectableLootSpawner
    {
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private LootItem _lootPrefab;
        [SerializeField] private int _lootCount = 3;
        [SerializeField] private bool _useSpawnInterval = true;
        [SerializeField] private float[] _spawnIntervals = new float[] { 0f, 0.03f, 0.05f };

        private WaitForSeconds[] _spawnIntervalWait;
        private ICollectable _collectable;

        private void Awake()
        {
            _spawnIntervalWait = new WaitForSeconds[_spawnIntervals.Length];
            for (int i = 0; i < _spawnIntervals.Length; i++)
                _spawnIntervalWait[i] = new WaitForSeconds(_spawnIntervals[i]);
            
            _collectable = GetComponent<ICollectable>();
        }

        public override void SpawnLoot(GameObject collectorGo)
        {
            var tool = collectorGo.GetComponent<ICharacterTool>();
            ILootCollector collector = null;
            if (tool != null)
            {
                if (tool is ILootCollector collectorTool && collectorTool.CanCollect)
                    collector = collectorTool;
                else
                    collector = tool.CharacterRoot.GetComponent<ILootCollector>();
            }
            else
            {
                collector = collectorGo.GetComponent<ILootCollector>();
            }
            
            if (collector == null)
                return;
            
            StartCoroutine(SpawnRoutine(collector));
        }

        private IEnumerator SpawnRoutine(ILootCollector collector)
        {
            for (int i = 0; i < _lootCount; i++)
            {
                var spawnPointInd = i % _spawnPoints.Length;
                var spawnPoint = _spawnPoints[spawnPointInd];

                if (_useSpawnInterval)
                {
                    var wait = i >= _spawnIntervalWait.Length
                        ? _spawnIntervalWait[_spawnIntervalWait.Length - 1]
                        : _spawnIntervalWait[i];
                    yield return wait;
                }

                var item = _lootPrefab.Spawn(spawnPoint.position, spawnPoint.rotation);
                item.NextSourcePrefab = _lootPrefab;
                collector.CollectLoot(item, _collectable, i);
            }
        }
    }
}