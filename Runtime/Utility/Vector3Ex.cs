using UnityEngine;

namespace JakePerry.Unity
{
    public static class Vector3Ex
    {
        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static void MinMax(ref Vector3 min, ref Vector3 max)
        {
            var minCopy = min;
            min = Vector3.Min(min, max);
            max = Vector3.Max(minCopy, max);
        }

        public static Vector3 Scalar(float value)
        {
            return new Vector3(value, value, value);
        }

        public static Vector3 UnitVec(int axis)
        {
            MathEx.PosMod(ref axis, 3);

            Vector3 vec = default;
            vec[axis] = 1;

            return vec;
        }

        public static Vector3 UnitVec(Axis axis)
        {
            return UnitVec((int)axis);
        }
    }
}
