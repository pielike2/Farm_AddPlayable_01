using System;
using UnityEngine;

namespace Utility
{
    // Have to serialize generics in derived classes since Playworks 6.4.0
    // [Serializable] public class MarkedTransformReference : MarkedReference<Transform> { }
    // [Serializable] public class MarkedObjectReference : MarkedReference<MarkedObject> { }
    // [Serializable] public class MarkedRefHolderReference : MarkedReference<ReferencesHolder> { }
    // [Serializable] public class MarkedAnimatorReference : MarkedReference<Animator> { }
    
    [Serializable]
    public class MarkedTransformReference : MarkedReference<Transform>
    {
        [SerializeField] private string _markId = "Undefined";
        [SerializeField] private string _nestedId = null;
        [SerializeField] private bool _skipInactive;

        protected override string __markId
        {
            get => _markId;
            set => _markId = value;
        }
        
        protected override string __nestedId
        {
            get => _nestedId;
            set => _nestedId = value;
        }
        
        protected override bool __skipInactive
        {
            get => _skipInactive;
            set => _skipInactive = value;
        }
    }

    [Serializable]
    public class MarkedObjectReference : MarkedReference<MarkedObject>
    {
        [SerializeField] private string _markId = "Undefined";
        [SerializeField] private string _nestedId = null;
        [SerializeField] private bool _skipInactive;

        protected override string __markId
        {
            get => _markId;
            set => _markId = value;
        }
        
        protected override string __nestedId
        {
            get => _nestedId;
            set => _nestedId = value;
        }
        
        protected override bool __skipInactive
        {
            get => _skipInactive;
            set => _skipInactive = value;
        }
    }

    [Serializable]
    public class MarkedRefHolderReference : MarkedReference<ReferencesHolder>
    {
        [SerializeField] private string _markId = "Undefined";
        [SerializeField] private string _nestedId = null;
        [SerializeField] private bool _skipInactive;

        protected override string __markId
        {
            get => _markId;
            set => _markId = value;
        }
        
        protected override string __nestedId
        {
            get => _nestedId;
            set => _nestedId = value;
        }
        
        protected override bool __skipInactive
        {
            get => _skipInactive;
            set => _skipInactive = value;
        }
    }

    [Serializable]
    public class MarkedAnimatorReference : MarkedReference<Animator>
    {
        [SerializeField] private string _markId = "Undefined";
        [SerializeField] private string _nestedId = null;
        [SerializeField] private bool _skipInactive;

        protected override string __markId
        {
            get => _markId;
            set => _markId = value;
        }
        
        protected override string __nestedId
        {
            get => _nestedId;
            set => _nestedId = value;
        }
        
        protected override bool __skipInactive
        {
            get => _skipInactive;
            set => _skipInactive = value;
        }
    }
}