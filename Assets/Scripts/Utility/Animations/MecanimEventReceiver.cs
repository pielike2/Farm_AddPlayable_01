using System;
using UnityEngine;

namespace Utility
{
    public class MecanimEventReceiver : MonoBehaviour
    {
        public event Action<int> OnReceiveInt; 
        public event Action<string> OnReceiveString; 
        
        public void API_ReceiveInt(int value)
        {
            OnReceiveInt?.Invoke(value);
        } 
        
        public void API_ReceiveString(string value)
        {
            OnReceiveString?.Invoke(value);
        }
    }
}