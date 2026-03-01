using UnityEngine;

namespace Playable.UI
{
    public enum UiTransitionTrigger
    {
        Open,
        Close
    }
    
    public abstract class BaseUiAnimation : MonoBehaviour
    {
        public virtual void Init()
        {
        }
        
        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }
        
        public void TriggerOpen()
        {
            PlayOpen();
        }

        public void TriggerClose()
        {
            PlayClose();
        }

        protected abstract void PlayOpen();
        
        protected abstract void PlayClose();

        public virtual float GetTransitionDuration(UiTransitionTrigger trigger)
        {
            return -1f;
        }
        
        public abstract void EndInstantly();
    }
}