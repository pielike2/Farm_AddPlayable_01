using DG.Tweening;

namespace Base
{
    public static class PlayablePluginsInitializer
    {
        public static void Init()
        {
            DOTween.SetTweensCapacity(250, 250);
        }
    }
}