using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// A class derived from <see cref="RuntimeSettingsBase"/> can be decorated with this attribute
    /// to specify the display path in the settings menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RuntimeSettingsPathAttribute : Attribute
    {
        private readonly string m_path;

        /// <summary>
        /// Indicates the display path in the settings menu.
        /// </summary>
        public string Path => m_path;

        /// <summary>
        /// Relative display order of this object in the editor window.
        /// <para/>
        /// This is only used if more than one settings object is displayed
        /// at the same path.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <param name="path">
        /// Display path in the settings menu.
        /// </param>
        public RuntimeSettingsPathAttribute(string path)
        {
            m_path = path;
        }
    }
}
