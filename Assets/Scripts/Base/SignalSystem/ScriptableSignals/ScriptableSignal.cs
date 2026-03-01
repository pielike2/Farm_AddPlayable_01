using UnityEngine;

namespace Base.SignalSystem
{
    [CreateAssetMenu(menuName = "ScriptableSignal", fileName = "New ScriptableSignal")]
    public class ScriptableSignal : ScriptableObject
    {
        public void Trigger()
        {
            Get.SignalBus.Publish(new SScriptableSignal(this));
        }
        
        public void Trigger(params object[] data)
        {
            Get.SignalBus.Publish(new SScriptableSignal(this, data));
        }
    }
    
    public readonly struct SScriptableSignal
    {
        public ScriptableSignal Signal { get; }
        public object[] Data { get; }

        public SScriptableSignal(ScriptableSignal e, params object[] data)
        {
            Signal = e;
            Data = data;
        }
    }
}