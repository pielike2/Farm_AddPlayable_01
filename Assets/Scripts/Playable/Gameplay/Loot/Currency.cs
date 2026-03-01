using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.Loot
{
    public class Currency : MonoBehaviour
    {
        [SerializeField] private HashId _currencyId;
        [SerializeField] private int _defaultValuePerItem = 5;
        
        public HashId CurrencyId => _currencyId;
        public float Value { get; private set; }
        public float ValuePerItem => _defaultValuePerItem;

        public event Action<float, float> OnValueChanged;

        private static readonly Dictionary<int, Currency> _currencies = new Dictionary<int, Currency>();

        private void Awake()
        {
            _currencies[_currencyId.Hash] = this;
        }

        private void OnDestroy()
        {
            if (_currencies.TryGetValue(_currencyId.Hash, out var currency) && currency == this)
                _currencies.Remove(_currencyId.Hash);
        }

        public void AddCurrencyAmount(float amount)
        {
            if (amount <= 0f)
                return;
            Value += amount;
            OnValueChanged?.Invoke(Value - amount, Value);
        }

        public void WasteCurrency(float amount)
        {
            if (amount <= 0f)
                return;
            Value -= amount;
            if (Value <= 0.001f)
                Value = 0f;
            OnValueChanged?.Invoke(Value + amount, Value);
        }

        public bool TryWasteCurrency(float amount)
        {
            if (Value - amount < 0f)
                return false;
            WasteCurrency(amount);
            return true;
        }

        #region Static API

        public static Currency GetDefaultCurrency()
        {
            return _currencies.First().Value;
        }

        public static float GetDefaultCurrencyValue()
        {
            return _currencies.First().Value.Value;
        }

        public static int GetDefaultCurrencyId()
        {
            return _currencies.First().Value.CurrencyId.Hash;
        }

        public static float GetCurrencyAmount(int currencyIdHash)
        {
            return _currencies.TryGetValue(currencyIdHash, out var currency) ? currency.Value : 0;
        }

        public static bool TryGetCurrency(int currencyIdHash, out Currency currency)
        {
            return _currencies.TryGetValue(currencyIdHash, out currency);
        }
        
        public static bool TryAddCurrencyAmount(int currencyIdHash, float amount)
        {
            if (!_currencies.TryGetValue(currencyIdHash, out var currency))
                return false;
            currency.AddCurrencyAmount(amount);
            return true;
        }

        public static void WasteCurrency(int currencyIdHash, float amount)
        {
            if (_currencies.TryGetValue(currencyIdHash, out var currency))
                currency.WasteCurrency(amount);
        }

        public static bool TryWasteCurrency(int currencyIdHash, float amount)
        {
            return _currencies.TryGetValue(currencyIdHash, out var currency) &&
                   currency.TryWasteCurrency(amount);
        }

        #endregion
    }
}