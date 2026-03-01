using UnityEngine;

namespace Base.SignalSystem
{
    public class ScriptableSignalMonoTrigger : MonoBehaviour
    {
        public void API_Trigger(ScriptableSignal signal)
        {
            signal.Trigger(this);
        }
    }
}