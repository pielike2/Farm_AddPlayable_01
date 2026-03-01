using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using DG.Tweening;
using Playable.Animations;
using Playable.Gameplay.Character;
using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Placements
{
    public class Placement : MonoBehaviour, ISensorTarget
    {
        [SerializeField] private HashId _senseId = new HashId("Interactable");
        [SerializeField] private bool _needStopMovement = true;
        
        [Space(10)]
        [SerializeField] private Transform _placementRoot;
        
        [Space(10)]
        [SerializeField] private BaseSimpleTweenAnimation _placementShowAnim;
        [SerializeField] private BaseSimpleTweenAnimation _placementHideAnim;

        private bool _isInitialized;
        private Collider _collider;
        private HashSet<IInteractor> _interactors = new HashSet<IInteractor>();

        public HashId SenseId => _senseId;
        public bool IsColliderActive => _collider.enabled;
        public bool NeedStopMovement => _needStopMovement;
        
        public HashSet<IInteractor> Interactors => _interactors;
        
        public event Action<Placement, IInteractor> OnStartInteraction;
        public event Action<Placement, IInteractor> OnStopInteraction;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void StartInteraction(IInteractor interactor)
        {
            if (_interactors.Add(interactor))
                OnStartInteraction?.Invoke(this, interactor);
        }

        public void StopInteraction(IInteractor interactor)
        {
            if (_interactors.Remove(interactor))
                OnStopInteraction?.Invoke(this, interactor);

            if (_interactors.Count > 0)
                OnStartInteraction?.Invoke(this, _interactors.First());
        }

        public void Activate()
        {
            if (_placementRoot.gameObject.activeSelf)
                return;
            
            _placementRoot.DOKill();
            _placementRoot.gameObject.SetActive(true);
            _collider.enabled = false;

            if (_placementShowAnim == null)
            {
                OnCompleteShow();
                return;
            }
            
            var showSeq = _placementShowAnim.Animate(_placementRoot);
            showSeq.onComplete = OnCompleteShow;
        }

        public void Deactivate()
        {
            if (!_placementRoot.gameObject.activeSelf)
                return;
            _placementRoot.DOKill();
            _collider.enabled = false;
            if (_placementHideAnim == null)
            {
                OnCompleteHide();
                return;
            }
            
            var hideSeq = _placementHideAnim.Animate(_placementRoot);
            hideSeq.onComplete = OnCompleteHide;
        }

        private void OnCompleteHide()
        {
            _placementRoot.gameObject.SetActive(false);
        }

        private void OnCompleteShow()
        {
            _collider.enabled = true;
        }
    }
}