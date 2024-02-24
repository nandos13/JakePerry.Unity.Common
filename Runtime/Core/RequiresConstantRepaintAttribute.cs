using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// Decorate a PropertyDrawer type with this attribute to
    /// constantly repaint any editors currently using the drawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresConstantRepaintAttribute : Attribute { }
}
