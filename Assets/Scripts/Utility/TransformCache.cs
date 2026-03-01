using System;
using UnityEngine;

namespace Base
{
    [Serializable]
    public struct TransformCache
    {
        public Transform Transform { get; set; }
        public Transform Parent { get; set; }
        public bool IsValid { get; set; }
        public bool IsParentValid { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public TransformCache(Transform t)
        {
            IsValid = t != null;
            Transform = t;
            if (IsValid)
            {
                Position = t.localPosition;
                Rotation = t.localRotation;
                Scale = t.localScale;
                Parent = t.parent;
                IsParentValid = Parent != null;
            }
            else
            {
                Position = Vector3.zero;
                Rotation = Quaternion.identity;
                Scale = Vector3.one;
                Parent = null;
                IsParentValid = false;
            }
        }

        public TransformCache(Component component) : this()
        {
            if (component == null)
            {
                IsValid = false;
                return;
            }
            
            var t = component.transform;
            Transform = t;
            if (IsValid)
            {
                Position = t.localPosition;
                Rotation = t.localRotation;
                Scale = t.localScale;
            }
            else
            {
                Position = Vector3.zero;
                Rotation = Quaternion.identity;
                Scale = Vector3.one;
            }
        }

        public void ApplyCache(bool useParent = true)
        {
            if (!IsValid)
                return;
            
            if (useParent)
                Transform.SetParent(Parent);
            
            Transform.localPosition = Position;
            Transform.localRotation = Rotation;
            Transform.localScale = Scale;
        }
    }
}