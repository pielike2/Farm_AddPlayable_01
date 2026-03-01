using UnityEngine;

namespace Utility
{
    public static class SpringMath
    {
        private static float SpringVelocity(float currentValue, float targetValue, float velocity, float damping, float frequency, float deltaTime)
        {
            frequency = frequency * 2f * Mathf.PI;
            float f2 = frequency * frequency;
            float d2 = 2.0f * damping * frequency;
            float x = currentValue - targetValue;
            float acceleration = -f2 * x - d2 * velocity;
            velocity += deltaTime * acceleration;
            return velocity;
        }
        
        public static void Spring(ref float currentValue, float targetValue, ref float velocity, float damping, float frequency, float deltaTime)
        {
            float fixedDeltaTime = 1.0f / 60.0f; 
            float accumulator = deltaTime;
            while (accumulator > 0f)
            {
                float step = Mathf.Min(accumulator, fixedDeltaTime);
                velocity = SpringVelocity(currentValue, targetValue, velocity, damping, frequency, step);
                currentValue += step * velocity; 
                accumulator -= step;
            }
        }
    }
}