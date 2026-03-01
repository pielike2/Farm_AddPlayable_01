using UnityEngine;

namespace Playable.Configs
{
    public class VisualsConfig : ScriptableObject
    {
        public Sprite[] defaultDigitSprites = new Sprite[10];
        public float soundPlayRadiusInner = 7f;
        public float soundPlayRadiusOuter = 10f;
    }
}