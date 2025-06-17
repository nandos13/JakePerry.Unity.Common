using JakePerry.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JakePerry.Unity
{
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
    public static class UnityHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Correctness", "UNT0029")]
        private static bool IsNullReference(UnityEngine.Object obj)
        {
            return obj is null;
        }

        /// <summary>
        /// Evaluates the current state of the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="evaluateUnassignedRef">
        /// <i>Used in editor only.</i>
        /// Determines the return value of this method when <paramref name="obj"/> is an unassigned reference.
        /// <para/>
        /// If set to <see langword="true"/>, this method returns <see cref="UnityObjectState.UnassignedReference"/>;
        /// Otherwise it returns <see cref="UnityObjectState.NullReference"/>.
        /// </param>
        public static UnityObjectState GetObjectState(UnityEngine.Object obj, bool evaluateUnassignedRef = false)
        {
            if (IsNullReference(obj)) return UnityObjectState.NullReference;

            // If the object evaluates to null here, it is either a destroyed object,
            // or it is a deserialized unassigned reference (editor only).
            if (obj == null)
            {
#if UNITY_EDITOR
                if (obj.GetInstanceID() == 0)
                {
                    return evaluateUnassignedRef
                        ? UnityObjectState.UnassignedReference
                        : UnityObjectState.NullReference;
                }
#endif

                return UnityObjectState.DestroyedObject;
            }

            return UnityObjectState.ValidObject;
        }

        public static bool DoesObjectWithInstanceIDExist(int id)
        {
            const BindingFlags kFlags = (BindingFlags)0x28;
            const string kMethodName = "DoesObjectWithInstanceIDExist";

            MethodInfo method = ReflectionEx.GetMethod(typeof(UnityEngine.Object), kMethodName, kFlags, new ParamsArray<Type>(typeof(int)));

            using (ReflectionEx.RentArrayWithArgsInScope(out object[] args, id))
            {
                return (bool)method.Invoke(null, args);
            }
        }

        /// <summary>
        /// Find an object with a given instance id.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="id">The instance id to match.</param>
        public static T FindObjectFromInstanceId<T>(int id)
            where T : UnityEngine.Object
        {
            const BindingFlags kFlags = (BindingFlags)0x28;
            const string kMethodName = "FindObjectFromInstanceID";

            MethodInfo method = ReflectionEx.GetMethod(typeof(UnityEngine.Object), kMethodName, kFlags, new ParamsArray<Type>(typeof(int)));

            using (ReflectionEx.RentArrayWithArgsInScope(out object[] args, id))
            {
                return method.Invoke(null, args) is T o ? o : null;
            }
        }

        /// <inheritdoc cref="FindObjectFromInstanceId{T}(int)"/>
        public static UnityEngine.Object FindObjectFromInstanceId(int id)
        {
            return FindObjectFromInstanceId<UnityEngine.Object>(id);
        }

        /// <summary>
        /// Get a string representation of the <paramref name="guid"/> in the format used by Unity.
        /// </summary>
        /// <param name="guid">
        /// The input <see cref="Guid"/>.
        /// </param>
        /// <returns>
        /// A string of 32 hexadecimal digits representing the <paramref name="guid"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetUnityGuidString(Guid guid)
        {
            return guid.ToString("N");
        }

        /// <summary>
        /// Parse a guid string obtained from Unity API.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ParseUnityGuidString(string guidString)
        {
            Enforce.Argument(guidString, nameof(guidString)).IsNotNullOrEmpty();

            Enforce.Argument(guidString, nameof(guidString)).Condition(
                Guid.TryParseExact(guidString, "N", out Guid guid), "Failed to parse Guid.");

            return guid;
        }
    }
}
