using System;
using System.Reflection;
using UnityEngine.Events;

namespace JakePerry.Unity.Events
{
    // TODO: Documentation
    internal static class UnityEventToolsWrapper
    {
        private static Type Type_UnityEventTools => UnityInternalsHelper.GetType(typeof(UnityEvent).Assembly, "UnityEngine.Events.UnityEventTools");

        internal static string TidyAssemblyTypeName(string assemblyTypeName)
        {
            const BindingFlags kFlags = BindingFlags.Static | BindingFlags.NonPublic;

            var types = new ParamsArray<Type>(typeof(string));

            var method = UnityInternalsHelper.GetMethod(Type_UnityEventTools, "TidyAssemblyTypeName", kFlags, types);
            return (string)method.Invoke(null, new object[1] { assemblyTypeName });
        }
    }
}
