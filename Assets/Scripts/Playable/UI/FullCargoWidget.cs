using System;
using System.Collections.Generic;
using Base;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay;
using Playable.Signals;
using UnityEngine;
using UnityEngine.UI;
using Utility;
using Utility.Extensions;

namespace Playable.UI
{
    public class FullCargoWidget : BaseScreenConstraintWidget
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _animDelay = 0.2f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private BaseSimpleTweenAnimation _originPosAnimation;
        [SerializeField] private MarkedTransformReference _originPointRef;
        [SerializeField] private List<BaseCargo> _cargos = new List<BaseCargo>();

        private Sequence _sequence;
        private IDisposable _sub;

        public override bool ConstraintScale => true;
        public override bool ConstraintRotation => true;
        public override Transform OriginPoint => _originPointRef.Value;
        public override bool IsOriginValid => _originPointRef.IsValid;

        private void Awake()
        {
            // _collector.OnCollectStart += OnCollectStart;
            _image.gameObject.SetActive(false);
            _sub = Get.SignalBus.Subscribe<SCargoIsFull>(e =>
            {
                if (_cargos.Contains(e.Cargo) && _sequence == null)
                    PlayAnimation();
            });
        }

        private void OnDestroy()
        {
            // _collector.OnCollectStart -= OnCollectStart;
            _sub.Dispose();
        }

        // private void OnCollectStart(ILootItem lootItem)
        // {
        //     if (_collector.ActiveCargo.IsFull && _sequence == null)
        //         PlayAnimation();
        // }

        private void PlayAnimation()
        {
            _sequence?.Kill(false);

            enabled = true;
            
            _image.gameObject.SetActive(true);
            _image.color = _image.color.SetAlpha(0f);
            
            _sequence = DOTween.Sequence();
            if (_originPointRef.IsValid)
                _sequence.Append(_originPosAnimation.Animate(_originPointRef.Value));
            _sequence.Join(_image.DOFade(1f, _originPosAnimation.Duration).SetEase(_fadeCurve));
            _sequence.PrependInterval(_animDelay);
            
            _sequence.onComplete = () =>
            {
                enabled = false;
                transform.ZeroLocals();
                if (_originPointRef.IsValid)
                    _originPointRef.Value.ZeroLocals();
                _image.gameObject.SetActive(false);
                _sequence = null;
            };
        }
    }
}