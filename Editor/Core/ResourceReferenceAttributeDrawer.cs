using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(ResourceReferenceAttribute))]
    public sealed class ResourceReferenceAttributeDrawer : PropertyDrawer
    {
        private static bool ShowErrorContent(ref Rect position, string tooltip, bool warn = false)
        {
            Rect iconRect = position.PadLeft(position.width - position.height);
            position = position.PadRight(iconRect.width + Spacing);

            Texture2D icon = UnityEditorHelper.GetMessageIcon(warn ? MessageType.Warning : MessageType.Error);
            GUIStyle iconStyle = EditorStyles.iconButton;
            Rect iconRect2 = iconStyle.margin.Remove(iconRect);

            GUIContent content = new(icon) { tooltip = tooltip };
            EditorGUI.LabelField(iconRect2, content, iconStyle);

            Event evt = Event.current;
            return evt.shift
                && evt.control
                && evt.type == EventType.MouseDown
                && iconRect.Contains(evt.mousePosition);
        }

        private static void CopyResourcesPath(object o)
        {
            PackedGuid guid = (PackedGuid)o;
            if (UnityEditorHelper.TryGetResourcesPathFromAssetGuid(guid, out string resourcePath))
            {
                GUIUtility.systemCopyBuffer = resourcePath;
            }
        }

        private static void CopyAssetsPath(object o)
        {
            string assetPath = (string)o;
            GUIUtility.systemCopyBuffer = assetPath;
        }

        private static void AddToManifest(object o)
        {
            PackedGuid guid = (PackedGuid)o;
            if (UnityEditorHelper.TryGetResourcesPathFromAssetGuid(guid, out string resourcePath))
            {
                ResourceGuidManifest manifest = ResourceGuidManifestEditorUtil.GetOrCreateManifestAsset();

                manifest.Editor_AddToCache(guid, resourcePath);

                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssetIfDirty(manifest);
            }
        }

        private void ShowContextMenu(PackedGuid guid, SerializedProperty property)
        {
            bool gotAssetPath = UnityEditorHelper.TryGetProjectAssetPath(guid, out string assetPath);
            bool isResource = ResourcesEx.TryGetResourcesPath(assetPath, out string resourcePath);

            GenericMenu menu = new();

            GuidEditorUtil.AddCopyGuidCommand(menu, guid, "Copy Guid");

            GenericMenu.MenuFunction2 copyResourcesFunc =
                (gotAssetPath && isResource) ? CopyResourcesPath : null;

            menu.AddItem(new GUIContent("Copy Resources-relative Path"), false, copyResourcesFunc, guid);

            GenericMenu.MenuFunction2 copyAssetsFunc =
                gotAssetPath ? CopyAssetsPath : null;

            menu.AddItem(new GUIContent("Copy Assets-relative Path"), false, copyAssetsFunc, assetPath);

            menu.AddSeparator(null);

            bool isResouceButIsMissingFromManifest =
                gotAssetPath &&
                isResource &&
                (!ResourceGuidManifest.Editor_TryGetResourcePathNoEditorFallback(guid, out string manifestPath) || !StringComparer.Ordinal.Equals(manifestPath, resourcePath));

            GenericMenu.MenuFunction2 addToManifestFunc =
                isResouceButIsMissingFromManifest ? AddToManifest : null;

            menu.AddItem(new GUIContent("Add to Resources manifest"), false, addToManifestFunc, guid);

            menu.AddSeparator(null);

            GuidEditorUtil.AddClearGuidCommand(menu, property);

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        private bool DrawAssetField(Rect position, ref PackedGuid guid)
        {
            ResourceReferenceAttribute attr = (ResourceReferenceAttribute)attribute;
            Type resourceType = attr.ResourceType;

            string assetPath;
            UnityEngine.Object asset = null;

            // Validate the attribute's ResourceType restiction
            if (!typeof(UnityEngine.Object).IsAssignableFrom(resourceType))
            {
                string err = $"{nameof(ResourceReferenceAttribute)} has an invalid type restiction. ResourceType must be assignable to type UnityEngine.Object.";
                ShowErrorContent(ref position, err);
            }
            else if (!guid.IsDefaultValue)
            {
                // Check if asset is missing
                if (!UnityEditorHelper.TryGetProjectAssetPath(guid, out assetPath))
                {
                    string err = "Asset could not be found.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err, warn: true))
                    {
                        string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                        Debug.LogError(
                            "A GUID is assigned but the asset was not found. This may indicate that an asset was previously assigned and has since been deleted from the project.\n" +
                            $"- GUID: {unityGuidString}");
                    }
                }
                // Check the asset is of the expected type
                else if (!resourceType.IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath)))
                {
                    string err = "Asset is unexpected type.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err, warn: true))
                    {
                        string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                        Debug.LogError(
                            "A GUID is assigned but the corresponding asset is an unexpected type.\n" +
                            $"- GUID: {unityGuidString}\n- Asset type: {AssetDatabase.GetMainAssetTypeAtPath(assetPath)}\n- Expected type: {resourceType}",
                            AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object)));
                    }
                }
                // Check the asset is in a Resources directory
                else if (!ResourcesEx.TryGetResourcesPath(assetPath, out string resourcePath))
                {
                    string err = "Asset is not a Resource.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err))
                    {
                        string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                        Debug.LogError(
                            "A GUID is assigned but the corresponding asset is not part of a Resources directory.\n" +
                            $"- GUID: {unityGuidString}\n- Asset path: {assetPath}",
                            AssetDatabase.LoadAssetAtPath(assetPath, resourceType));
                    }
                }
                else
                {
                    asset = AssetDatabase.LoadAssetAtPath(assetPath, resourceType);

                    // Check the asset is included in the manifest to be loadable
                    if (!ResourceGuidManifest.Editor_TryGetResourcePathNoEditorFallback(guid, out _))
                    {
                        string err = "Asset is missing from Resources manifest.\nFor debug info, ctrl + shift + click.";
                        if (ShowErrorContent(ref position, err, warn: true))
                        {
                            string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                            Debug.LogError(
                                "The GUID corresponds to a Resources asset but the asset is not included in the Resources manifest. This will cause a failure in build.\n" +
                                $"- GUID: {unityGuidString}\n- Asset path: {assetPath}",
                                asset);
                        }
                    }
                }
            }

            EditorGUI.BeginChangeCheck();

            UnityEngine.Object newObj = EditorGUI.ObjectField(position, asset, resourceType, allowSceneObjects: false);

            if (!EditorGUI.EndChangeCheck())
                return false;

            if (newObj == null)
            {
                return TryGet.Pass(default, out guid);
            }

            // Disallow directly referencing a directory asset
            if (newObj is DefaultAsset)
            {
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(newObj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (ResourcesEx.IsResourcesPath(assetPath))
                {
                    return TryGet.Pass(new PackedGuid(AssetDatabase.AssetPathToGUID(assetPath)), out guid);
                }
                Debug.LogError("Error: Asset is not in a Resources folder.", newObj);
            }
            else
            {
                Debug.LogError("Error: Failed to get asset path for the selected asset.", newObj);
            }

            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // For reasons I can't comprehend, rect height is 2 pixels larger when drawing an array element
            position.height = GetPropertyHeight(property, label);

            position = EditorGUI.PrefixLabel(position, label);

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                ValueMemberInfo member = PropertyPathWalker.GetFieldOrProperty(property);

                if (member.MemberType != typeof(PackedGuid) &&
                    member.MemberType != typeof(PackedGuid[]) &&
                    member.MemberType != typeof(List<PackedGuid>))
                {
                    const string kText = "Incorrect member type";
                    const string kTooltip = nameof(ResourceReferenceAttribute) + " should only be used with the " + nameof(PackedGuid) + " type";

                    ShowErrorContent(ref position, kTooltip);
                    EditorGUI.LabelField(position, new GUIContent(kText, kTooltip), EditorStyles.boldLabel);
                    return;
                }

                Rect optionsRect = new RectOffset((int)(position.width - position.height - Spacing), 0, 0, 0).Remove(position);
                position = position.PadRight(optionsRect.width + Spacing);

                PackedGuid guid = GuidEditorUtil.GetGuid(property);

                if (DrawAssetField(position, ref guid))
                {
                    GuidEditorUtil.SetGuid(property, guid);
                }

                if (EditorGUIEx.ThreeDotMenuButton(optionsRect))
                {
                    ShowContextMenu(guid, property);
                }
            }
        }
    }
}
