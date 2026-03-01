using UnityEngine;

namespace Playable.Configs
{
    public class RegistryConfig : ScriptableObject
    {
        public ServicesConfig services;
        public CommonConfig common;
        public BalanceConfig balance;
        public VisualsConfig visuals;
    }
}
