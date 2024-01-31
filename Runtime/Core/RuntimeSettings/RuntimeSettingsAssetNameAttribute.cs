using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// A class derived from <see cref="RuntimeSettingsBase"/> can be decorated with this attribute
    /// to specify the name of the asset in the project.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RuntimeSettingsAssetNameAttribute : Attribute
    {
        private readonly string m_assetName;

        /// <summary>
        /// Indicates the name of the settings asset in the project.
        /// </summary>
        public string AssetName => m_assetName;

        /// <param name="assetName">
        /// Name of the settings asset in the project.
        /// </param>
        public RuntimeSettingsAssetNameAttribute(string assetName)
        {
            m_assetName = assetName;
        }
    }
}
