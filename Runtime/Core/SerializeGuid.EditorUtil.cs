#if UNITY_EDITOR
using UnityEditor;

namespace JakePerry.Unity
{
    public partial struct SerializeGuid
    {
        /// <summary>
        /// Provides convenient helper methods for working with
        /// <see cref="SerializeGuid"/> in the editor.
        /// </summary>
        public static class EditorUtil
        {
            internal static void GetGuidParts(
                SerializedProperty property,
                out SerializedProperty a,
                out SerializedProperty b)
            {
                a = property.FindPropertyRelative("_a");
                b = property.FindPropertyRelative("_b");
            }

            /// <summary>
            /// Get a guid value.
            /// </summary>
            /// <param name="property">A <see cref="SerializeGuid"/> property.</param>
            /// <returns>The guid value stored in the property.</returns>
            public static SerializeGuid GetGuid(SerializedProperty property)
            {
                GetGuidParts(property, out SerializedProperty a, out SerializedProperty b);

                SerializeGuid guid;
                unchecked { guid = SerializeGuid.Deserialize((ulong)a.longValue, (ulong)b.longValue); }

                return guid;
            }

            /// <summary>
            /// Set a guid value.
            /// </summary>
            /// <param name="property">A <see cref="SerializeGuid"/> property.</param>
            /// <param name="guid">The value to set.</param>
            public static void SetGuid(SerializedProperty property, SerializeGuid guid)
            {
                GetGuidParts(property, out SerializedProperty a, out SerializedProperty b);

                long la, lb;
                unchecked
                {
                    la = (long)guid.SegmentA;
                    lb = (long)guid.SegmentB;
                }

                a.longValue = la;
                b.longValue = lb;
            }

            /// <summary>
            /// Attempt to find an asset in the project with the given guid.
            /// </summary>
            /// <param name="guid">The guid to match.</param>
            /// <param name="asset">The asset, or <see langword="null"/> if none was found.</param>
            /// <returns>
            /// <see langword="true"/> if a matching asset was found; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool TryFindAsset(SerializeGuid guid, out UnityEngine.Object asset)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid.UnityGuidString);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null)
                        return true;
                }

                asset = null;
                return false;
            }

            /// <summary>
            /// Attempt to get the guid of an asset in the project.
            /// </summary>
            /// <param name="asset">The project asset.</param>
            /// <param name="guid">The asset's guid, or <see langword="default"/> if none was found.</param>
            /// <returns>
            /// <see langword="true"/> if the guid was obtained; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool TryGetGuidFromAsset(UnityEngine.Object asset, out SerializeGuid guid)
            {
                guid = default;
                if (asset == null)
                {
                    return false;
                }

                var path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    guid = new SerializeGuid(AssetDatabase.GUIDFromAssetPath(path).ToString());
                    return true;
                }

                return false;
            }
        }
    }
}

#endif // UNITY_EDITOR
