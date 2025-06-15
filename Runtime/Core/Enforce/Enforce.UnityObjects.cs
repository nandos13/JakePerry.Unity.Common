using System;
using System.Runtime.CompilerServices;

namespace JakePerry.Unity
{
    public static class EnforceUnityMethods
    {
        /// <summary>
        /// Assert that the argument is not null or destroyed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull<T>(this in Enforce.ArgumentContainer<T> c)
            where T : UnityEngine.Object
        {
            UnityObjectState state = UnityHelper.GetObjectState(c.value, evaluateUnassignedRef: true);

            if (state.IsNullReference)
            {
                throw new ArgumentNullException(c.parameterName);
            }

            // Note: Only applicable in editor, so we can skip this check for performance in builds.
#if UNITY_EDITOR
            if (state.IsUnassigned)
            {
                throw new ArgumentNullException(c.parameterName, "Deserialized reference was unassigned in the inspector.");
            }
#endif
        }
    }
}
