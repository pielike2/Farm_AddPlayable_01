using Base;
using Playable.Animations;
using UnityEngine;

namespace Playable.Gameplay.Loot
{
    public class CargoLootUtility : MonoBehaviour
    {
        [SerializeField] private AudioClipShell _collectSfx;
        [SerializeField] private float _cargoItemScale = 0.8f;
        [SerializeField] private float _cargoItemAnimationCountThreshold = 0.8f;
        [SerializeField] private BaseLootCollectAnimation _collectAnimation;
        [SerializeField] private BaseSimpleTweenAnimation _itemAppearAnimation;

        public AudioClipShell CollectSfx => _collectSfx;
        public float CargoItemScale => _cargoItemScale;
        public float CargoItemAnimationCountThreshold => _cargoItemAnimationCountThreshold;
        public BaseLootCollectAnimation CollectAnimation => _collectAnimation;
        public BaseSimpleTweenAnimation ItemAppearAnimation => _itemAppearAnimation;
    }
}