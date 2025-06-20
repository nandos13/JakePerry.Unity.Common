#if UNITY_EDITOR

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class UnityEditorHelper
    {
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct GuidConverter
        {
            [FieldOffset(0)]
            public Guid systemGuid;

            [FieldOffset(0)]
            public GUID unityGuid;

            [FieldOffset(0)] private int a;
            [FieldOffset(0)] private byte a1;
            [FieldOffset(1)] private byte a2;
            [FieldOffset(2)] private byte a3;
            [FieldOffset(3)] private byte a4;

            [FieldOffset(4)] private short b;
            [FieldOffset(4)] private byte b1;
            [FieldOffset(5)] private byte b2;

            [FieldOffset(6)] private short c;
            [FieldOffset(6)] private byte c1;
            [FieldOffset(7)] private byte c2;

            [FieldOffset(8)] private byte d;
            [FieldOffset(9)] private byte e;
            [FieldOffset(10)] private byte f;
            [FieldOffset(11)] private byte g;
            [FieldOffset(12)] private byte h;
            [FieldOffset(13)] private byte i;
            [FieldOffset(14)] private byte j;
            [FieldOffset(15)] private byte k;

            /// <summary>
            /// Break a byte into segments of 4 low bits &amp; 4 high bits, then swap the segments.
            /// </summary>
            private static byte Swizzle4(byte b)
            {
                byte low = (byte)(b & 0x0F);
                byte high = (byte)(b >> 4);

                return (byte)((low << 4) + high);
            }

            /// <summary>
            /// Swizzle the bytes of the Guid to convert from one format to the other.
            /// </summary>
            public void Swizzle()
            {
                a = BinaryPrimitives.ReverseEndianness(a);
                b = BinaryPrimitives.ReverseEndianness(b);
                c = BinaryPrimitives.ReverseEndianness(c);

                a1 = Swizzle4(a1);
                a2 = Swizzle4(a2);
                a3 = Swizzle4(a3);
                a4 = Swizzle4(a4);

                b1 = Swizzle4(b1);
                b2 = Swizzle4(b2);

                c1 = Swizzle4(c1);
                c2 = Swizzle4(c2);

                d = Swizzle4(d);
                e = Swizzle4(e);
                f = Swizzle4(f);
                g = Swizzle4(g);
                h = Swizzle4(h);
                i = Swizzle4(i);
                j = Swizzle4(j);
                k = Swizzle4(k);
            }
        }

        /// <summary>
        /// Find all assets of the given type <typeparamref name="T"/> in the project.
        /// </summary>
        public static T[] FindObjectsInProject<T>() where T : UnityEngine.Object
        {
            string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            return assetGuids
                .Select(static guid =>
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    return AssetDatabase.LoadAssetAtPath<T>(path);
                })
                .Where(static a => a != null)
                .ToArray();
        }

        public static Texture2D GetMessageIcon(MessageType messageType)
        {
            MethodInfo method = typeof(EditorGUIUtility).GetMethod("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic);
            object result = method.Invoke(null, new object[] { messageType });

            return (Texture2D)result;
        }

        /// <summary>
        /// Convert a regular <see cref="Guid"/> struct to Unity's <see cref="GUID"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GUID ToUnityGuid(Guid guid)
        {
            GuidConverter c = new() { systemGuid = guid };
            c.Swizzle();

            return c.unityGuid;
        }

        /// <summary>
        /// Convert an instance of Unity's <see cref="GUID"/> struct to a regular <see cref="Guid"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ToSystemGuid(GUID guid)
        {
            GuidConverter c = new() { unityGuid = guid };
            c.Swizzle();

            return c.systemGuid;
        }

        /// <summary>
        /// Get the <see cref="Guid"/> of the given project <paramref name="asset"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Guid"/> of <paramref name="asset"/> if it was found in the
        /// <see cref="AssetDatabase"/>; Otherwise, <see langword="null"/>.
        /// </returns>
        public static Guid? GetProjectAssetGuid(UnityEngine.Object asset)
        {
            Enforce.Argument(asset, nameof(asset)).IsNotNull();

            if (!AssetDatabase.Contains(asset))
            {
                return null;
            }

            Debug.Assert(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guidString, out long _));

            return UnityHelper.ParseUnityGuidString(guidString);
        }

        /// <summary>
        /// Get the path of the asset with the given <paramref name="guid"/>.
        /// </summary>
        /// <param name="guid">
        /// The Guid of a project asset.
        /// </param>
        /// <returns>
        /// The path of the project asset with the given <paramref name="guid"/>, relative to the
        /// project folder, if one was found; Otherwise, an empty string.
        /// </returns>
        public static string GetProjectAssetPath(Guid guid)
        {
            GUID unityGuid = ToUnityGuid(guid);
            return AssetDatabase.GUIDToAssetPath(unityGuid);
        }

        /// <summary>
        /// Get the asset with the given <paramref name="guid"/>.
        /// </summary>
        /// <param name="guid">
        /// The Guid of a project asset.
        /// </param>
        /// <returns>
        /// The project asset with the given <paramref name="guid"/>, if one was found;
        /// Otherwise, <see langword="null"/>.
        /// </returns>
        public static T GetProjectAsset<T>(Guid guid)
            where T : UnityEngine.Object
        {
            string path = GetProjectAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return null;
        }

        /// <inheritdoc cref="GetProjectAsset{T}"/>
        public static UnityEngine.Object GetProjectAsset(Guid guid)
        {
            return GetProjectAsset<UnityEngine.Object>(guid);
        }

        /// <summary>
        /// <inheritdoc cref="GetProjectAssetGuid" path="/summary"/>
        /// </summary>
        /// <param name="guid">
        /// When this method returns, contains the <see cref="Guid"/> of the given
        /// <paramref name="asset"/>, if it was found; Otherwise, contains the default
        /// <see cref="Guid"/> instance.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="Guid"/> was found;
        /// Otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetProjectAssetGuid(UnityEngine.Object asset, out Guid guid)
        {
            Guid? result = GetProjectAssetGuid(asset);
            return result.TryGetValue(out guid);
        }

        /// <summary>
        /// <inheritdoc cref="GetProjectAssetPath" path="/summary"/>
        /// </summary>
        /// <param name="guid">
        /// <inheritdoc cref="GetProjectAssetPath" path="/param[@name='guid']"/>
        /// </param>
        /// <param name="assetPath">
        /// Path of the asset relative to the project folder.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an asset was found;
        /// Otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetProjectAssetPath(Guid guid, out string assetPath)
        {
            assetPath = GetProjectAssetPath(guid);
            return !string.IsNullOrEmpty(assetPath);
        }

        /// <summary>
        /// <inheritdoc cref="GetProjectAsset{T}" path="/summary"/>
        /// </summary>
        /// <param name="guid">
        /// <inheritdoc cref="GetProjectAsset{T}" path="/param[@name='guid']"/>
        /// </param>
        /// <param name="asset">
        /// When this method returns, contains the asset matching the <paramref name="guid"/>
        /// if one was found; Otherwise, contains <see langword="null"/>.
        /// </param>
        public static bool TryGetProjectAsset<T>(Guid guid, out T asset)
            where T : UnityEngine.Object
        {
            asset = GetProjectAsset<T>(guid);
            return asset != null;
        }

        /// <inheritdoc cref="TryGetProjectAsset{T}"/>
        public static bool TryGetProjectAsset(Guid guid, out UnityEngine.Object asset)
        {
            return TryGetProjectAsset<UnityEngine.Object>(guid, out asset);
        }

        /// <summary>
        /// Attempts to find the Resources-relative path for an asset with the given guid.
        /// </summary>
        /// <param name="guid">Guid of the resource asset.</param>
        /// <inheritdoc cref="ResourcesEx.TryGetResourcesPath(string, out string)"/>
        public static bool TryGetResourcesPathFromAssetGuid(Guid guid, out string resourcePath)
        {
            resourcePath = string.Empty;
            return TryGetProjectAssetPath(guid, out string assetPath)
                && ResourcesEx.TryGetResourcesPath(assetPath, out resourcePath);
        }
    }
}

#endif // UNITY_EDITOR
