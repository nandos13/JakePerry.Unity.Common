using JakePerry.Reflection;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace JakePerry.Unity
{
    /// <summary>
    /// Contains helper methods. Use the generic <see cref="InspectableInterface{T}"/>
    /// to serialize an inspectable interface reference.
    /// </summary>
    public static class InspectableInterface
    {
        /// <summary>
        /// Cast object <paramref name="o"/> to the given interface type <typeparamref name="T"/>,
        /// if the cast can be made.
        /// </summary>
        /// <param name="o">The Unity object to cast.</param>
        /// <returns>
        /// The Unity object as an instance of the interface type, if it implements
        /// the interface; Otherwise, <see langword="null"/>.
        /// </returns>
        public static T CastUnityObjectToInterface<T>(UnityEngine.Object o)
            where T : class
        {
            return o is T cast ? cast : null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Provides convenient helper methods for working with
        /// <see cref="InspectableInterface{T}"/> in the editor.
        /// </summary>
        public static class EditorUtil
        {
            /// <summary>
            /// Get the child property that references an <see cref="UnityEngine.Object"/>
            /// implementing the interface.
            /// </summary>
            /// <param name="property">An <see cref="InspectableInterface{T}"/> property.</param>
            /// <returns>
            /// The child property that serializes a reference to an <see cref="UnityEngine.Object"/>.
            /// </returns>
            public static SerializedProperty GetTargetObjectProperty(SerializedProperty property)
            {
                return property.FindPropertyRelative("m_targetObject");
            }

            /// <summary>
            /// Attempt to cast the <see cref="UnityEngine.Object"/> referenced by the serialized
            /// <see cref="InspectableInterface{T}"/> to the given <paramref name="interfaceType"/>,
            /// if the cast can be made.
            /// </summary>
            /// <param name="property">An <see cref="InspectableInterface{T}"/> property.</param>
            /// <inheritdoc cref="CastUnityObjectToInterface{T}(UnityEngine.Object)"/>
            public static object GetReferencedInterface(SerializedProperty property, Type interfaceType)
            {
                const BindingFlags kFlags = BindingFlags.Static | BindingFlags.Public;
                const string kMethodName = nameof(CastUnityObjectToInterface);

                SerializedProperty targetProperty = GetTargetObjectProperty(property);
                UnityEngine.Object targetObj = targetProperty.objectReferenceValue;

                // We can exit early if the target object is not assigned
                if (UnityHelper.GetObjectState(targetObj).IsNull) return null;

                ParamsArray<Type> args = new(typeof(UnityEngine.Object));
                MethodInfo method = ReflectionEx.GetMethod(typeof(InspectableInterface), kMethodName, kFlags, args);

                args = new ParamsArray<Type>(interfaceType);
                method = ReflectionEx.MakeGenericMethod(method, args);

                using (ReflectionEx.RentArrayWithArgsInScope(out object[] args2, targetObj))
                {
                    return method.Invoke(null, args2);
                }
            }

            /// <summary>
            /// Get the referenced <see cref="UnityEngine.Object"/> 
            /// </summary>
            /// <param name="property">An <see cref="InspectableInterface{T}"/> property.</param>
            /// <returns>
            /// The <see cref="UnityEngine.Object"/> referenced by the <paramref name="data"/>
            /// which implements the interface.
            /// </returns>
            public static UnityEngine.Object GetTargetObject(SerializedProperty property)
            {
                SerializedProperty targetProperty = GetTargetObjectProperty(property);
                return targetProperty.objectReferenceValue;
            }
        }
    }
#endif // UNITY_EDITOR
}
