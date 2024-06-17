using System;
using System.Reflection;

namespace JakePerry.Unity
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Correctness", "UNT0029:Pattern matching with null on Unity objects")]
    public static class UnityHelper
    {
        /// <summary>
        /// Determines whether <paramref name="obj"/> is unassigned - that is,
        /// if it is a <see langword="null"/> reference.
        /// This method also checks for Unity's "fake" objects in editor (see details below).
        /// <para/>
        /// Any 'null' references to an <see cref="UnityEngine.Object"/> or any of its child
        /// classes that are deserialized while running in the editor are assigned
        /// a fake object instance. This fake object allows Unity to alert the user if they attempt to
        /// access the referenced object with this undoubtedly familiar message:
        /// <para/>
        /// <c>The variable *Variable* of *Script* has not been assigned. You probably need
        /// to assign the *Variable* variable of the *Script* script in the inspector.</c>
        /// </summary>
        /// <param name="obj">
        /// The object reference to be checked.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the object is unassigned, per the summary above;
        /// Otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsUnassigned(UnityEngine.Object obj)
        {
            if (obj is null) return true;

#if UNITY_EDITOR
            if (obj.GetInstanceID() == 0) return true;
#endif // UNITY_EDITOR

            return false;
        }

        /// <summary>
        /// Check that an argument is not null or destroyed.
        /// </summary>
        /// <param name="obj">
        /// The argument to be checked.
        /// </param>
        /// <param name="paramName">
        /// Name of the argument being checked.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="obj"/> is a null reference (unassigned).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="obj"/> is destroyed.
        /// </exception>
        public static void CheckArgument(UnityEngine.Object obj, string paramName)
        {
            if (obj is null)
                throw new ArgumentNullException(paramName);

            if (obj == null)
                throw new ArgumentException($"Object is destroyed.", paramName);
        }

        public static bool DoesObjectWithInstanceIDExist(int id)
        {
            const BindingFlags kFlags = (BindingFlags)0x28;
            const string kMethodName = "DoesObjectWithInstanceIDExist";

            var method = ReflectionEx.GetMethod(typeof(UnityEngine.Object), kMethodName, kFlags, new ParamsArray<Type>(typeof(int)));

            var args = ReflectionEx.RentArrayWithArguments(id);
            bool result = (bool)method.Invoke(null, args);

            ReflectionEx.ReturnArray(args);
            return result;
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

            var method = ReflectionEx.GetMethod(typeof(UnityEngine.Object), kMethodName, kFlags, new ParamsArray<Type>(typeof(int)));

            var args = ReflectionEx.RentArrayWithArguments(id);
            var result = method.Invoke(null, args) is T o ? o : null;

            ReflectionEx.ReturnArray(args);
            return result;
        }

        /// <inheritdoc cref="FindObjectFromInstanceId{T}(int)"/>
        public static UnityEngine.Object FindObjectFromInstanceId(int id)
        {
            return FindObjectFromInstanceId<UnityEngine.Object>(id);
        }
    }
}
