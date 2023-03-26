using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static partial class EditorGUIEx
    {
        /// <summary>
        /// Returns the internal 'CurrentStyles' instance.
        /// </summary>
        public static EditorStyles CurrentStyles => (EditorStyles)typeof(EditorStyles).GetField("s_Current", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        /// <summary>
        /// Obtains an editor style via reflection.
        /// </summary>
        /// <param name="fieldName">The name of the non-static <see cref="EditorStyles"/> field.</param>
        /// <returns>
        /// The <see cref="GUIStyle"/> stored under the given field name.
        /// </returns>
        public static GUIStyle GetStyle(string fieldName)
        {
            return (GUIStyle)typeof(EditorStyles).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(CurrentStyles);
        }
    }
}
