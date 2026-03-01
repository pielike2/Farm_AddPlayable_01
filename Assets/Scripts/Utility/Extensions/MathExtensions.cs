using UnityEngine;

namespace Utility.Extensions
{
    public static class MathExtensions
    {
        public static float Remap (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        
        public static float ClampBottom(this float f, float min)
        {
            return f < min ? min : f;
        }

        public static float ClampTop(this float f, float max)
        {
            return f > max ? max : f;
        }
        
        public static Vector3 SetX(this Vector3 v, float x)
        {
            v.x = x;
            return v;
        }

        public static Vector3 SetY(this Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        public static Vector3 SetZ(this Vector3 v, float z)
        {
            v.z = z;
            return v;
        }

        public static Vector2 SetX(this Vector2 v, float x)
        {
            v.x = x;
            return v;
        }

        public static Vector2 SetY(this Vector2 v, float y)
        {
            v.y = y;
            return v;
        }
    }
}