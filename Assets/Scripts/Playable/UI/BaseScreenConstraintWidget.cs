using Base;
using UnityEngine;

namespace Playable.UI
{
    public abstract class BaseScreenConstraintWidget : MonoBehaviour
    {
        public virtual bool IsOriginValid => true;
        public virtual Transform OriginPoint => null;
        public virtual bool ConstraintScale => false;
        public virtual bool ConstraintRotation => false;

        protected virtual void Update()
        {
        }
        
        protected virtual void LateUpdate()
        {
            UpdateScreenConstraint();
        }

        protected virtual void UpdateScreenConstraint()
        {
            if (IsOriginValid)
            {
                transform.position = Get.SceneVars.mainCamera.WorldToScreenPoint(OriginPoint.position);
                if (ConstraintScale)
                    transform.localScale = OriginPoint.localScale;
                if (ConstraintRotation)
                    transform.localRotation = OriginPoint.localRotation;
            }
        }
    }
}