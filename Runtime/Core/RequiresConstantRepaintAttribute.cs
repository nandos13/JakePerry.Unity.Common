using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// An attribute which can be used to force constant repainting of a PropertyDrawer
    /// or an EditorWindow.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresConstantRepaintAttribute : Attribute { }
}
