using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Playable.UI
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleEventHandler : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        
        [SerializeField] private GameObject _stateOn;
        [SerializeField] private GameObject _stateOff;
        
        [SerializeField] private UnityEvent _onActivated;
        [SerializeField] private UnityEvent _onDeactivated;

        private bool _statesValid;

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Awake()
        {
            _toggle.onValueChanged.AddListener(OnValueChanged);
            _statesValid = _stateOn != null &&
                           _stateOff != null;
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool value)
        {
            if (_statesValid)
            {
                _stateOn.SetActive(value);
                _stateOff.SetActive(!value);
            }
            
            if (value)
                _onActivated.Invoke();
            else
                _onDeactivated.Invoke();
        }
    }
}