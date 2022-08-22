using UnityEngine;

using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace JakePerry.Unity
{
    public static class DebugEx
    {
        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Debug(
            Vector3 c0,
            Vector3 c1,
            Vector3 c2,
            Vector3 c3,
            Vector3 c4,
            Vector3 c5,
            Vector3 c6,
            Vector3 c7,
            Color color,
            float duration,
            bool depthTest)
        {
            // Bottom
            Debug.DrawLine(c0, c1, color, duration, depthTest);
            Debug.DrawLine(c0, c2, color, duration, depthTest);
            Debug.DrawLine(c1, c3, color, duration, depthTest);
            Debug.DrawLine(c2, c3, color, duration, depthTest);

            // Top
            Debug.DrawLine(c4, c5, color, duration, depthTest);
            Debug.DrawLine(c4, c6, color, duration, depthTest);
            Debug.DrawLine(c5, c7, color, duration, depthTest);
            Debug.DrawLine(c6, c7, color, duration, depthTest);

            // Vertical
            Debug.DrawLine(c0, c4, color, duration, depthTest);
            Debug.DrawLine(c1, c5, color, duration, depthTest);
            Debug.DrawLine(c2, c6, color, duration, depthTest);
            Debug.DrawLine(c3, c7, color, duration, depthTest);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Gizmos(
            Vector3 c0,
            Vector3 c1,
            Vector3 c2,
            Vector3 c3,
            Vector3 c4,
            Vector3 c5,
            Vector3 c6,
            Vector3 c7)
        {
            // Bottom
            Gizmos.DrawLine(c0, c1);
            Gizmos.DrawLine(c0, c2);
            Gizmos.DrawLine(c1, c3);
            Gizmos.DrawLine(c2, c3);

            // Top
            Gizmos.DrawLine(c4, c5);
            Gizmos.DrawLine(c4, c6);
            Gizmos.DrawLine(c5, c7);
            Gizmos.DrawLine(c6, c7);

            // Vertical
            Gizmos.DrawLine(c0, c4);
            Gizmos.DrawLine(c1, c5);
            Gizmos.DrawLine(c2, c6);
            Gizmos.DrawLine(c3, c7);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Debug(this Box3D b, Color color, float duration = 0, bool depthTest = true)
        {
            Vector3 c0, c1, c2, c3, c4, c5, c6, c7;
            b.GetCorners(out c0, out c1, out c2, out c3, out c4, out c5, out c6, out c7);

            DrawBox_Debug(c0, c1, c2, c3, c4, c5, c6, c7, color, duration, depthTest);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Gizmos(this Box3D b)
        {
            Vector3 c0, c1, c2, c3, c4, c5, c6, c7;
            b.GetCorners(out c0, out c1, out c2, out c3, out c4, out c5, out c6, out c7);

            DrawBox_Gizmos(c0, c1, c2, c3, c4, c5, c6, c7);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Debug(this BoxUnaligned3D b, Color color, float duration = 0, bool depthTest = true)
        {
            Vector3 c0, c1, c2, c3, c4, c5, c6, c7;
            b.GetCorners(out c0, out c1, out c2, out c3, out c4, out c5, out c6, out c7);

            DrawBox_Debug(c0, c1, c2, c3, c4, c5, c6, c7, color, duration, depthTest);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Gizmos(this BoxUnaligned3D b)
        {
            Vector3 c0, c1, c2, c3, c4, c5, c6, c7;
            b.GetCorners(out c0, out c1, out c2, out c3, out c4, out c5, out c6, out c7);

            DrawBox_Gizmos(c0, c1, c2, c3, c4, c5, c6, c7);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Debug(this Bounds b, Color color, float duration = 0, bool depthTest = true)
        {
            DrawBox_Debug((Box3D)b, color, duration, depthTest);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawBox_Gizmos(this Bounds b)
        {
            DrawBox_Gizmos((Box3D)b);
        }
    }
}
