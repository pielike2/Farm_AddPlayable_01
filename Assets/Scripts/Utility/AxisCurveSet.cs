using System;
using UnityEngine;

namespace Utility
{
    [Serializable]
    public class AxisCurveSet
    {
        public AnimationCurve X = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public AnimationCurve Y = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public AnimationCurve Z = AnimationCurve.EaseInOut(0, 0, 1, 0);

        public static AxisCurveSet EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            return new AxisCurveSet
            {
                X = AnimationCurve.EaseInOut(timeStart, valueStart, timeEnd, valueEnd),
                Y = AnimationCurve.EaseInOut(timeStart, valueStart, timeEnd, valueEnd),
                Z = AnimationCurve.EaseInOut(timeStart, valueStart, timeEnd, valueEnd)
            };
        }

        public static AxisCurveSet Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            return new AxisCurveSet
            {
                X = AnimationCurve.Linear(timeStart, valueStart, timeEnd, valueEnd),
                Y = AnimationCurve.Linear(timeStart, valueStart, timeEnd, valueEnd),
                Z = AnimationCurve.Linear(timeStart, valueStart, timeEnd, valueEnd)
            };
        }

        public Vector3 Evaluate(float time)
        {
            return new Vector3(X.Evaluate(time), Y.Evaluate(time), Z.Evaluate(time));
        }
    }
}