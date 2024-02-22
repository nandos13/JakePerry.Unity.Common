using System;
using System.Buffers;

namespace JakePerry.Unity
{
    public static class TypeSerializationUtility
    {
        /// <summary>
        /// Get a tidied version of the given assembly qualified type name
        /// that contains the minimum required data for serialization to work.
        /// </summary>
        /// <param name="assemblyTypeName">
        /// The assembly qualified name of a type, as given by <see cref="Type.AssemblyQualifiedName"/>.
        /// </param>
        /// <returns>
        /// Tidied string containing only the type's full name &amp; assembly name.
        /// </returns>
        /// <remarks>
        /// This is a reimplementation of Unity's internal UnityEventTools.TidyAssemblyTypeName method
        /// with minor performance improvements.
        /// </remarks>
        public static string TidyTypeNameForSerialization(string assemblyTypeName)
        {
            if (string.IsNullOrEmpty(assemblyTypeName))
            {
                return string.Empty;
            }

            var span = assemblyTypeName.AsSpan();

            // Remove Version, Culture, PublicKeyToken data which we don't care about.
            int i = span.IndexOf(", Version=", StringComparison.Ordinal);
            if (i > -1) span = span[..i];

            i = span.IndexOf(", Culture=", StringComparison.Ordinal);
            if (i > -1) span = span[..i];

            i = span.IndexOf(", PublicKeyToken=", StringComparison.Ordinal);
            if (i > -1) span = span[..i];

            /* Unity's types are a special case.
             * Unity's assemblies are all named in format UnityEngine.XModule (ie. CoreModule, AndroidJNIModule, etc).
             * This code strips module assembly name such that serialization isn't broken if the type is moved to a different module.
             * Per Unity's internal UnityEventTools.TidyAssemblyTypeName method comments...
             * "The non-modular version will always work, due to type forwarders."
             */

            const string kUnityModuleData = ", UnityEngine";

            i = span.IndexOf(kUnityModuleData + ".", StringComparison.Ordinal);
            if (i != -1 && span.EndsWith("Module", StringComparison.Ordinal))
            {
                span = span[..i];

                var buffer = ArrayPool<char>.Shared.Rent(span.Length + kUnityModuleData.Length);
                span.CopyTo(buffer);
                kUnityModuleData.AsSpan().CopyTo(buffer.AsSpan(span.Length));

                var result = new string(buffer);

                ArrayPool<char>.Shared.Return(buffer);

                return result;
            }

            // We don't need to allocate a new string if the span is equal to the original.
            return span.Length == assemblyTypeName.Length
                ? assemblyTypeName
                : span.ToString();
        }
    }
}
