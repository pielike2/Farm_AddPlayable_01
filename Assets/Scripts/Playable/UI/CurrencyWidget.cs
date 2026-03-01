using DG.Tweening;
using Playable.Gameplay.Loot;
using UnityEngine;
using Utility;

namespace Playable.UI
{
    public class CurrencyWidget : MonoBehaviour
    {
        [SerializeField] private ImageNumberWidget _numberWidget;
        [SerializeField] private HashId _currencyId = new HashId("Money");
        [SerializeField] private float _numberTweenDuration = 0.3f;

        private float _displayedCount;
        private Tween _numberTween;
        private Currency _currency;
        
        private void Awake()
        {
            Currency.TryGetCurrency(_currencyId.Hash, out _currency);
            if (_currency != null)
            {
                _numberWidget.SetNumber((int)_currency.Value);
                _currency.OnValueChanged += OnValueChanged;
            }
            else
            {
                _numberWidget.SetNumber(0);
            }
        }

        private void OnDestroy()
        {
            if (_currency != null)
                _currency.OnValueChanged -= OnValueChanged;
        }

        private void OnValueChanged(float prevValue, float newValue)
        {
            if (_numberTween != null)
                _numberTween.Kill(false);
            _numberTween = DOVirtual.Float(_displayedCount, newValue, _numberTweenDuration,
                value =>
                {
                    _displayedCount = value;
                    _numberWidget.SetNumber((int)value);
                }).SetEase(Ease.InOutQuad);
            _numberTween.onKill = () => _numberTween = null;
        }
    }
}