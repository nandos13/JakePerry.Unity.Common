using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// Decorate a <see cref="SerializeTypeDefinition"/> field with this attribute to
    /// disallow assigning an open generic type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class DisallowUnboundGenericTypeAttribute : Attribute { }
}
