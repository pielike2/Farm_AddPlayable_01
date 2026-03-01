using System;
using UnityEngine;

namespace Utility
{
    [Serializable]
    public class HashId
    {
#if UNITY_EDITOR
        [SerializeField] private string _id;
        public string Id => _id;
#endif
        [SerializeField] private int _hash;
        public int Hash => _hash;

        public HashId()
        {
        }

        public HashId(string id)
        {
            SetId(id);
        }

        public void SetId(string id)
        {
#if UNITY_EDITOR
            _id = id;
#endif
            _hash = HashUtil.StringToHash(id);
        }

        public static bool operator ==(HashId left, HashId right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            
            return left._hash == right._hash;
        }

        public static bool operator !=(HashId left, HashId right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is HashId other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }
}