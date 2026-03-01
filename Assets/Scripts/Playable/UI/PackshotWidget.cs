using DG.Tweening;
using Playable.Animations;
using UnityEngine;
using UnityEngine.UI;
using Utility.Extensions;

namespace Playable.UI
{
    public class PackshotWidget : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image[] _logoLetters;
        [SerializeField] private BaseSimpleTweenAnimation _logoLetterAppearAnim;
        [SerializeField] private AnimationCurve _logoLetterFadeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float _logoLetterInterval = 0.1f;
        [SerializeField] private Animation _animation;
        [SerializeField] private GameObject _ctaButton;
        [SerializeField] private float _ctaButtonDelay = 0.35f;

        public void Play()
        {
            this.DOKill();
            
            _root.SetActive(true);
            _animation.Rewind();
            _animation.Play();
            
            for (int i = 0; i < _logoLetters.Length; i++)
            {
                var seq = _logoLetterAppearAnim.Animate(_logoLetters[i].rectTransform);
                _logoLetters[i].color = Color.white.SetAlpha(0f);
                seq.Join(_logoLetters[i].DOFade(1f, _logoLetterAppearAnim.Duration).SetEase(_logoLetterFadeCurve));
                seq.PrependInterval(_logoLetterInterval * i);
            }

            DOVirtual.DelayedCall(_ctaButtonDelay, () => _ctaButton.SetActive(true))
                .SetTarget(this);
        }

        public void Stop()
        {
            _root.SetActive(false);
            _animation.Rewind();
            
            for (int i = 0; i < _logoLetters.Length; i++)
            {
                _logoLetters[i].DOKill(true);
                _logoLetters[i].transform.DOKill(true);
            }
        }
    }
}