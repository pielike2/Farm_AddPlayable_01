using Base;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Playable.UI
{
    public class JoystickWidget : MonoBehaviour
    {
        [SerializeField] private RectTransform _stick;
        [SerializeField] private float _maxDistance = 50f;
        [SerializeField] private GameObject _joystickRoot;
        [SerializeField] private JoystickTutorialWidget _joystickTutorial;
        [SerializeField] public Canvas _canvas;

        private RectTransform _selfRect;
        private Vector2 _initialPosition;

        void Awake()
        {
            _selfRect = GetComponent<RectTransform>();
            _joystickRoot.SetActive(false);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !Get.Input.CharacterControlBlocker.IsActive && !EventSystem.current.IsPointerOverGameObject())
            {
                var isOverObject = false;
#if UNITY_EDITOR
                isOverObject = EventSystem.current.IsPointerOverGameObject();
#else
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    isOverObject = true;
#endif
                if (!isOverObject)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform,
                        Input.mousePosition, _canvas.worldCamera, out var pos);
                    _selfRect.anchoredPosition = pos;
                    _initialPosition = pos;
                    _joystickRoot.SetActive(true);
                    _joystickTutorial.ForceDeactivate();
                }
            }
        
            if (Input.GetMouseButton(0) && gameObject.activeSelf && _joystickRoot.activeSelf)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, Input.mousePosition, _canvas.worldCamera, out var currentPos);
            
                var delta = currentPos - _initialPosition;
                var deltaMagnitude = delta.magnitude;
                if (deltaMagnitude > _maxDistance)
                    delta = delta / deltaMagnitude * _maxDistance;

                Get.Input.MovementVector = delta / _maxDistance;
            
                _stick.anchoredPosition = delta;
            }
        
            if (Input.GetMouseButtonUp(0) || Get.Input.CharacterControlBlocker.IsActive)
            {
                _stick.anchoredPosition = Vector2.zero;
                _joystickRoot.SetActive(false);
                Get.Input.MovementVector = Vector2.zero;
            }
        }
    }
}
