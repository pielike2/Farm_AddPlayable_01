using UnityEngine;

namespace Base
{
    public class AudioClipShellPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClipShell _startWorkSfx;
        [SerializeField] private bool _playByDistance;

        public void Play()
        {
            if (!_playByDistance)
                _startWorkSfx.Play();
            else _startWorkSfx.TryPlayByDistance(transform.position);
        }
    }
}