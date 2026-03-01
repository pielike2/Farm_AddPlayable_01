using UnityEngine;

namespace Base
{
    public static class TimeScaleManager
    {
        private static float _baseTimeScale = 1f;
        private static float _editorCheatsMultiplier = 1f;
        private static float _keyboardSpeedupMultiplier = 1f;

        public static float BaseTimeScale
        {
            get => _baseTimeScale;
            set
            {
                _baseTimeScale = value;
                Apply();
            }
        }

        public static float EditorCheatsMultiplier
        {
            get => _editorCheatsMultiplier;
            set
            {
                _editorCheatsMultiplier = value;
                Apply();
            }
        }

        public static float KeyboardSpeedupMultiplier
        {
            get => _keyboardSpeedupMultiplier;
            set
            {
                _keyboardSpeedupMultiplier = value;
                Apply();
            }
        }

        public static void Reset()
        {
            _baseTimeScale = 1f;
            _editorCheatsMultiplier = 1f;
            _keyboardSpeedupMultiplier = 1f;
            Apply();
        }

        private static void Apply()
        {
            var result = _baseTimeScale * _editorCheatsMultiplier * _keyboardSpeedupMultiplier;
            Time.timeScale = Mathf.Clamp(result, 0.1f, 10f);
        }
    }
}