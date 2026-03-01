using System;
using Base.PoolingSystem;
using UnityEngine;
using Utility.Extensions;

namespace Playable.UI
{
    public enum UiElementState
    {
        Opened,
        Closed,
        Closing,
        Opening
    }
    
    public class UiElement : MonoBehaviour
    {
        [SerializeField] private BaseUiAnimation[] _uiAnimations = new BaseUiAnimation[0];
        [SerializeField] private bool _isOpenedInitially;

        private bool _isInitialized;
        private IMonoPoolObject _poolObject;
        private bool _isPoolObjectValid;
        private Coroutine _transitionCoroutine;
        
        public UiElementState State { get; private set; }

        public bool IsActive => State == UiElementState.Opened || 
                                State == UiElementState.Opening;
        public bool IsReleasing { get; private set; } 

        public event Action<UiElement> OnStartOpen;
        public event Action<UiElement> OnStartClose;
        public event Action<UiElement> OnEndOpen;
        public event Action<UiElement> OnEndClose;

        public event Action<UiElement> OnNextStartClose;
        public event Action<UiElement> OnNextStartOpen;
        public event Action<UiElement> OnNextEndOpen;
        public event Action<UiElement> OnNextEndClose;
        
        private void Awake()
        {
            TryInit();
        }

        private void TryInit()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            
            _poolObject = GetComponent<IMonoPoolObject>();
            _isPoolObjectValid = _poolObject != null;

            for (int i = 0; i < _uiAnimations.Length; i++)
                _uiAnimations[i].Init();
            
            if (_isOpenedInitially)
            {
                State = UiElementState.Closed;
                Open(instantly: true);
            }
            else
            {
                State = UiElementState.Opened;
                Close(instantly: true);
            }
        }

        [ContextMenu("Open")]
        public void API_Open()
        {
            Open(false);
        }

        [ContextMenu("Close")]
        public void API_Close()
        {
            Close(false);
        }

        public void Open(bool instantly = false)
        {
            TryInit();
            
            if (State == UiElementState.Opened || State == UiElementState.Opening)
                return;
            
            StopTransition();
            
            State = UiElementState.Opening;
            gameObject.SetActive(true);
            
            OnStartOpen?.Invoke(this);
            OnNextStartOpen?.Invoke(this);
            
            if (instantly || _uiAnimations.Length == 0)
            {
                State = UiElementState.Opened;
                OnOpenTransitionEnd();
            }
            else
            {
                for (int i = 0; i < _uiAnimations.Length; i++)
                    _uiAnimations[i].TriggerOpen();
                
                _transitionCoroutine = this.After(GetDuration(UiTransitionTrigger.Open), e =>
                {
                    e.OnOpenTransitionEnd();
                });
            }
        }

        public void Close(bool instantly = false)
        {
            TryInit();
            
            if (State == UiElementState.Closed || State == UiElementState.Closing)
                return;
            
            StopTransition();
            
            OnStartClose?.Invoke(this);
            OnNextStartClose?.Invoke(this);
            
            if (instantly || _uiAnimations.Length == 0)
            {
                State = UiElementState.Closed;
                OnCloseTransitionEnd();
            }
            else
            {
                State = UiElementState.Closing;
                for (int i = 0; i < _uiAnimations.Length; i++)
                    _uiAnimations[i].TriggerClose();
                
                _transitionCoroutine = this.After(GetDuration(UiTransitionTrigger.Open), e =>
                {
                    e.OnCloseTransitionEnd();
                });
            }
        }

        private void StopTransition()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
        }

        public void CloseAndRelease()
        {
            IsReleasing = true;
            Close();
            if (_isPoolObjectValid)
                OnNextEndClose += _ => { _poolObject.Release(); };
        }

        private void OnOpenTransitionEnd()
        {
            State = UiElementState.Opened;
            _transitionCoroutine = null;
            OnEndOpen?.Invoke(this);
            OnNextEndOpen?.Invoke(this);
            OnNextEndOpen = null;
        }

        private void OnCloseTransitionEnd()
        {
            State = UiElementState.Closed;
            _transitionCoroutine = null;
            OnEndClose?.Invoke(this);
            OnNextEndClose?.Invoke(this);
            OnNextEndClose = null;
            gameObject.SetActive(false);
        }
        
        public float GetDuration(UiTransitionTrigger trigger)
        {
            if (_uiAnimations.Length == 0)
                return -1;
            var max = 0f;
            for (int i = 0; i < _uiAnimations.Length; i++)
            {
                var d = _uiAnimations[i].GetTransitionDuration(trigger);
                if (d > max)
                    max = d;
            }
            return max;
        }
    }
}