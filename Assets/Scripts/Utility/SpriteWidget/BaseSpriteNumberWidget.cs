using Base;
using UnityEngine;

namespace Utility
{
    public class BaseSpriteNumberWidget : MonoBehaviour
    {
        [SerializeField] private Sprite[] _customDigitSprites = new Sprite[0];
        
        protected virtual int RenderersCount => 0;

        protected virtual void SetRendererActive(int i, bool active) { }

        protected virtual void SetRendererSprite(int i, Sprite sprite) { }

        protected virtual Transform GetSpriteTransform(int i) { return null; }
        
        public void SetNumber(int number)
        {
            var digitSprites = Get.RegistryConfig.visuals.defaultDigitSprites;

            if (_customDigitSprites.Length >= 9)
                digitSprites = _customDigitSprites;

            if (number == 0)
            {
                SetRendererActive(0, true);
                SetRendererSprite(0, digitSprites[0]);
                for (int i = 1; i < RenderersCount; i++)
                    SetRendererActive(i, false);
                PostprocessNumber(1);
                return;
            }

            var temp = number;
            var digitCount = 0;
            while (temp > 0)
            {
                digitCount++;
                temp /= 10;
            }

            if (digitCount > RenderersCount)
            {
                var extraDigits = digitCount - RenderersCount;
                var divisorForTrunc = 1;
                for (int i = 0; i < extraDigits; i++)
                    divisorForTrunc *= 10;
                number /= divisorForTrunc;
                digitCount = RenderersCount;
            }

            // starting divisor: 10^(digitCount-1)
            var divisor = 1;
            for (int i = 1; i < digitCount; i++)
                divisor *= 10;

            var remaining = number;
            for (int i = 0; i < digitCount; i++)
            {
                var digit = remaining / divisor;
                remaining %= divisor;
                divisor /= 10;

                SetRendererActive(i, true);
                SetRendererSprite(i, digitSprites[digit]);
            }

            for (int i = digitCount; i < RenderersCount; i++)
                SetRendererActive(i, false);
            
            PostprocessNumber(digitCount);
        }

        protected virtual void PostprocessNumber(int activeDigits)
        {
        }
    }
}