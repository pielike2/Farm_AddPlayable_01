using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using Base.PoolingSystem;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Character;
using Playable.Gameplay.Loot;
using UnityEngine;
using UnityEngine.Events;
using Utility;
using Utility.Extensions;

namespace Playable.Gameplay.Placements
{
    public interface IPurchasePlacement
    {
        bool IsPurchaseCompleted { get; }
        Dictionary<int, float> FullPrice { get; }
        Dictionary<int, float> RemainingPrice { get; }
    }

    public class PurchaseZone : MonoBehaviour, IPurchasePlacement
    {
        [SerializeField] private Placement _placement;
        
        [SerializeField] private bool _customPricesEnabled;
        
        [SerializeField] private int _price = 100;
        [SerializeField] private BaseSpriteNumberWidget _numberWidget;
        [SerializeField] private HashId _paymentItemTypeId = new HashId("Money");
        [SerializeField] private BaseMovementTweenAnimation _itemAnim;
        [SerializeField] private AudioClipShell _putSfx;

        [SerializeField] private CurrencyPriceSettings _customPriceSettings = new CurrencyPriceSettings();
        
        [SerializeField] private Transform _targetPoint;
        [SerializeField] private float _interval = 0.1f;
        [SerializeField] private QuadSpriteWithProgress _fillSprite;
        [SerializeField] private Vector2 _fillRemap = new Vector2(0f, 1f);
        [SerializeField] private bool _skipFirstEnter;

        [SerializeField] private string _analyticsEventName = String.Empty;

        [Space(10)] [SerializeField] private UnityEvent<Placement> _onCompletePurchase;

        [Serializable]
        public class CurrencyPrice
        {
            public HashId paymentItemTypeId;
            public HashId currencyId;
            public int price;
            
            public BaseSpriteNumberWidget numberWidget;
            public BaseMovementTweenAnimation itemAnim;
            public AudioClipShell putSfx;
        }

        [Serializable]
        public class CurrencyPriceSettings
        {
            public CurrencyPrice[] prices = { };
        }

        private Coroutine _unloadCoroutine;
        private WaitForSeconds _intervalWait;
        private int _countEnters;
        private bool _priceInitialized;
        private float _commonPricePercent;
        private List<BaseCargo> _unloadCargos = new List<BaseCargo>();
        private readonly Dictionary<int, float> _fullPrice = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _remainingPrice = new Dictionary<int, float>();

        public bool IsPurchaseCompleted { get; private set; }
        public Placement Placement => _placement;

        public Dictionary<int, float> FullPrice
        {
            get
            {
                if (!_priceInitialized)
                    InitializePrice();
                return _fullPrice;
            }
        }
        public Dictionary<int, float> RemainingPrice
        {
            get
            {
                if (!_priceInitialized)
                    InitializePrice();
                return _remainingPrice;
            }
        }

        private void InitializePrice()
        {
            if (_priceInitialized)
                return;
            _priceInitialized = true;

            if (_customPricesEnabled)
            {
                foreach (var item in _customPriceSettings.prices)
                {
                    _fullPrice[item.currencyId.Hash] = item.price;
                    _remainingPrice[item.currencyId.Hash] = item.price;
                }
            }
            else
            {
                _fullPrice[Currency.GetDefaultCurrency().CurrencyId.Hash] = _price;
                _remainingPrice[Currency.GetDefaultCurrency().CurrencyId.Hash] = _price;
            }
            
            _commonPricePercent = 1f;
        }

        private void Reset()
        {
            _placement = GetComponent<Placement>();
        }

        private void Awake()
        {
            _placement.OnStartInteraction += OnStartInteraction;
            _placement.OnStopInteraction += OnStopInteraction;

            InitializePrice();
            
            var keys = new List<int>(_fullPrice.Keys);
            foreach (var key in keys)
            {
                _remainingPrice[key] = _fullPrice[key];
            }

            _priceInitialized = true;
            _intervalWait = new WaitForSeconds(_interval);
            RefreshNumberDisplay(true);
        }

        private void Start()
        {
            RefreshNumberDisplay(true);
        }

        private void OnStartInteraction(Placement placement, IInteractor interactor)
        {
            if (_unloadCoroutine != null)
                StopCoroutine(_unloadCoroutine);

            if (_skipFirstEnter && _countEnters++ <= 0)
                return;

            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();
            _unloadCargos.Clear();

            if (_customPricesEnabled)
            {
                for (var i = 0; i < _customPriceSettings.prices.Length; i++)
                    if (inventory.TryGetCargoByItemId(_customPriceSettings.prices[i].paymentItemTypeId, out var sourceCargo))
                        _unloadCargos.Add(sourceCargo);
            }
            else
            {
                if (inventory.TryGetCargoByItemId(_paymentItemTypeId, out var sourceCargo))
                    _unloadCargos.Add(sourceCargo);
            }
            
            if (_unloadCargos.Count > 0)
                _unloadCoroutine = StartCoroutine(UnloadRoutine());
        }

        private void OnStopInteraction(Placement placement, IInteractor interactor)
        {
            if (_unloadCoroutine != null)
                StopCoroutine(_unloadCoroutine);
        }

        private IEnumerator UnloadRoutine()
        {
            while (true)
            {
                if (_commonPricePercent <= Mathf.Epsilon)
                    yield break;

                if (Get.Input.PlacementPaymentBlocker.IsActive)
                {
                    yield return null;
                    continue;
                }

                for (int i = 0; i < _unloadCargos.Count; i++)
                {
                    if (_customPricesEnabled)
                    {
                        if (_remainingPrice.TryGetValue(_unloadCargos[i].ItemTypeId.Hash, out var remaining) && remaining <= Mathf.Epsilon)
                        {
                            _unloadCargos.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                    
                    if (_unloadCargos[i].TryReleaseTopItem(out var item, out _))
                        HandleUnloadedItem(item);
                }
                yield return _intervalWait;
            }
        }

        private void HandleUnloadedItem(Transform unloadedItem)
        {
            var unloadedLootItem = unloadedItem.GetComponent<ILootItem>();
            if (unloadedLootItem == null)
                return;
            
            Currency currency = null;
            BaseMovementTweenAnimation itemAnim = null;
            AudioClipShell putSfx = null;
            
            if (_customPricesEnabled)
            {
                if (unloadedLootItem.IsCurrencyItem)
                {
                    Currency.TryGetCurrency(unloadedLootItem.ItemTypeId.Hash, out currency);
                    for (var i = 0; i < _customPriceSettings.prices.Length; i++)
                        if (_customPriceSettings.prices[i].currencyId.Hash == currency.CurrencyId.Hash)
                        {
                            itemAnim = _customPriceSettings.prices[i].itemAnim;
                            putSfx = _customPriceSettings.prices[i].putSfx;
                        }
                }
            }
            else
            {
                currency = Currency.GetDefaultCurrency();
                itemAnim = _itemAnim;
                putSfx = _putSfx;
            }

            _remainingPrice[currency.CurrencyId.Hash] -= currency.ValuePerItem;
            if (_remainingPrice[currency.CurrencyId.Hash] < 0.001f)
                _remainingPrice[currency.CurrencyId.Hash] = 0f;

            var seq = itemAnim.Animate(unloadedItem, _targetPoint.position);
            seq.onComplete = () =>
            {
                unloadedLootItem.Release();

                CalculatePricePercent();
                
                RefreshNumberDisplay(false);

                if (_commonPricePercent < 0.001f)
                    CompletePurchase();
            };

            seq.InsertCallback(seq.Duration() * 0.5f, () =>
            {
                WasteCurrencyItem(unloadedLootItem);
                putSfx.Play();
            });
        }

        private void CalculatePricePercent()
        {
            _commonPricePercent = 0f;
            if (_customPricesEnabled)
            {
                for (var i = 0; i < _customPriceSettings.prices.Length; i++)
                {
                    _commonPricePercent += _remainingPrice[_customPriceSettings.prices[i].currencyId.Hash] /
                                           _fullPrice[_customPriceSettings.prices[i].currencyId.Hash];
                }
                _commonPricePercent /= _customPriceSettings.prices.Length;
            }
            else
            {
                var currency = Currency.GetDefaultCurrency();
                _commonPricePercent = _remainingPrice[currency.CurrencyId.Hash] / _fullPrice[currency.CurrencyId.Hash];
            }
        }

        private void WasteCurrencyItem(ILootItem unloadedLootItem)
        {
            if (_customPricesEnabled)
            {
                if (Currency.TryGetCurrency(unloadedLootItem.ItemTypeId.Hash, out var currency))
                    currency.WasteCurrency(currency.ValuePerItem);
            }
            else
            {
                Currency.GetDefaultCurrency().WasteCurrency(Get.Balance.dollarsPerPack);
            }
        }

        private void RefreshNumberDisplay(bool instant)
        {
            if (_customPricesEnabled)
            {
                for (var i = 0; i < _customPriceSettings.prices.Length; i++)
                    _customPriceSettings.prices[i].numberWidget.SetNumber((int)_remainingPrice[_customPriceSettings.prices[i].currencyId.Hash]);
            }
            else
            {
                _numberWidget.SetNumber((int)_remainingPrice[Currency.GetDefaultCurrency().CurrencyId.Hash]);
            }

            var fillPercent = 1f - _commonPricePercent;
            
            _fillSprite.DOKill();
            if (instant)
            {
                _fillSprite.Progress = fillPercent;
            }
            else
            {
                DOVirtual.Float(_fillSprite.Progress, fillPercent, 0.15f, value => { _fillSprite.Progress = value; })
                    .SetEase(Ease.OutCubic).SetTarget(_fillSprite);
            }
        }

        private void CompletePurchase()
        {
            if (IsPurchaseCompleted)
                return;
            IsPurchaseCompleted = true;

            _placement.Deactivate();

            _onCompletePurchase.Invoke(_placement);

            TrySendAnalyticsEvent();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_analyticsEventName) && _analyticsEventName.Contains(" "))
            {
                _analyticsEventName = _analyticsEventName.Replace(" ", "");
            }
        }
#endif

        private void TrySendAnalyticsEvent()
        {
            if (string.IsNullOrEmpty(_analyticsEventName))
                return;

            if (!Get.UsedAnalyticsEvents.Add(_analyticsEventName))
                return;

            Debug.Log($"[Analytics] {_analyticsEventName}");
        }
    }
}