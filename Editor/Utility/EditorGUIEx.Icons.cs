using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static partial class EditorGUIEx
    {
        public static class Icons
        {
            /* Note: Much easier to just grab the icon from a guid known ahead
             * of time than to find the location of the package & find it from there.
             */
            private const string kGuid_GameObject_Static = "7ce0e6b0b87f7964990b414cc7018f4a";

            private static Texture2D GetSymbolIcon(string guid)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            }

            public static Texture2D GameObjectStatic => GetSymbolIcon(kGuid_GameObject_Static);
        }
    }
}
