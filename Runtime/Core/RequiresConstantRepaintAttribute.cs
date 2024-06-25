using System;

#if UNITY_EDITOR
// Note: This is only included for documentation.
using UnityEditor;
#endif // UNITY_EDITOR

namespace JakePerry.Unity
{
    /// <summary>
    /// An attribute which can be used to force constant repainting of a <see cref="PropertyDrawer"/>
    /// or an <see cref="EditorWindow"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresConstantRepaintAttribute : Attribute
    {
        /// <summary>
        /// [Optional]
        /// Specifies the name of a method defined by the attributed type which is
        /// used to determine if forced repainting can be skipped to improve performance.
        /// <para/>
        /// Method specifications:
        /// <list type="bullet">
        /// <item>
        /// Method must be instance invocable (not static).
        /// </item>
        /// <item>
        /// Method must have return type <see langword="bool"/> &amp;
        /// return <see langword="false"/> if repainting can be skipped;
        /// Otherwise, <see langword="true"/> to trigger a repaint.
        /// </item>
        /// <item>
        /// When this attribute decorates a <see cref="PropertyDrawer"/> type, method signature
        /// can have zero arguments, <b>or</b> accept one argument of type <see cref="SerializedObject"/>.
        /// </item>
        /// <item>
        /// When this attribute decorates an <see cref="EditorWindow"/> type, method signature
        /// must have zero arguments.
        /// </item>
        /// </list>
        /// Example:
        /// <code>
        /// [RequiresConstantRepaint(If = "IsAnimating")]
        /// public class MyWindow : EditorWindow
        /// {
        ///     private UnityEditor.AnimatedValues.AnimBool m_value;
        ///     
        ///     private bool IsAnimating() => m_value.isAnimating;
        /// }
        /// </code>
        /// </summary>
        public string If { get; set; } = null;
    }
}
