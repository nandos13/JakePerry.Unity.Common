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
            //var iconRect = position.WithWidth(position.height);
            //position = position.PadLeft(iconRect.width + Spacing);
            var iconRect = position.PadLeft(position.width - position.height);
            position = position.PadRight(iconRect.width + Spacing);

            var icon = UnityEditorHelper.GetMessageIcon(warn ? MessageType.Warning : MessageType.Error);
            var iconStyle = EditorStyles.iconButton;
            var iconRect2 = iconStyle.margin.Remove(iconRect);

            var content = new GUIContent(icon) { tooltip = tooltip };
            EditorGUI.LabelField(iconRect2, content, iconStyle);

            var evt = Event.current;
            return evt.shift
                && evt.control
                && evt.type == EventType.MouseDown
                && iconRect.Contains(evt.mousePosition);
        }

        private static void CopyResourcesPath(object o)
        {
            var guid = (SerializeGuid)o;
            if (UnityEditorHelper.TryGetResourcesPath(guid, out string resourcePath))
            {
                GUIUtility.systemCopyBuffer = resourcePath;
            }
        }

        private static void CopyAssetsPath(object o)
        {
            var assetPath = (string)o;
            GUIUtility.systemCopyBuffer = assetPath;
        }

        private void ShowContextMenu(SerializeGuid guid, SerializedProperty property)
        {
            bool gotAssetPath = UnityEditorHelper.TryGetAssetPath(guid, out string assetPath);

            var menu = new GenericMenu();

            SerializeGuid.EditorUtil.AddCopyGuidCommand(menu, guid, "Copy Guid");

            var copyResourcesFunc = gotAssetPath && UnityEditorHelper.IsResourcesPath(assetPath)
                ? (GenericMenu.MenuFunction2)CopyResourcesPath
                : null;
            menu.AddItem(new GUIContent("Copy Resources-relative Path"), false, copyResourcesFunc, guid);

            var copyAssetsFunc = gotAssetPath
                ? (GenericMenu.MenuFunction2)CopyAssetsPath
                : null;
            menu.AddItem(new GUIContent("Copy Assets-relative Path"), false, copyAssetsFunc, assetPath);

            menu.AddSeparator(null);

            SerializeGuid.EditorUtil.AddClearGuidCommand(menu, property);

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        private bool DrawAssetField(Rect position, ref SerializeGuid guid)
        {
            var attr = (ResourceReferenceAttribute)attribute;
            var resourceType = attr.ResourceType;

            string assetPath, resourcePath;
            UnityEngine.Object asset = null;

            // Validate the attribute's ResourceType restiction
            if (!typeof(UnityEngine.Object).IsAssignableFrom(resourceType))
            {
                var err = $"{nameof(ResourceReferenceAttribute)} has an invalid type restiction. ResourceType must be assignable to type UnityEngine.Object.";
                ShowErrorContent(ref position, err);
            }
            else if (!guid.IsDefault)
            {
                // Check if asset is missing
                if (!UnityEditorHelper.TryGetAssetPath(guid, out assetPath))
                {
                    var err = "Asset could not be found.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err, warn: true))
                    {
                        Debug.LogError(
                            "A GUID is assigned but the asset was not found. This may indicate that an asset was previously assigned and has since been deleted from the project.\n" +
                            $"- GUID: {guid.UnityGuidString}");
                    }
                }
                // Check the asset is of the expected type
                else if (!resourceType.IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath)))
                {
                    var err = "Asset is unexpected type.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err, warn: true))
                    {
                        Debug.LogError(
                            "A GUID is assigned but the corresponding asset is an unexpected type. Click this log to ping the asset.\n" +
                            $"- GUID: {guid.UnityGuidString}\n- Asset type: {AssetDatabase.GetMainAssetTypeAtPath(assetPath)}\n- Expected type: {resourceType}",
                            asset);
                    }
                }
                // Check the asset is in a Resources directory
                else if (!UnityEditorHelper.TryGetResourcesPath(assetPath, out resourcePath))
                {
                    var err = "Asset is not a Resource.\nFor debug info, ctrl + shift + click.";
                    if (ShowErrorContent(ref position, err))
                    {
                        Debug.LogError(
                            "A GUID is assigned but the corresponding asset is not part of a Resources directory. Click this log to ping the asset.\n" +
                            $"- GUID: {guid.UnityGuidString}\n- Asset path: {assetPath}",
                            asset);
                    }
                }
                else
                {
                    asset = AssetDatabase.LoadAssetAtPath(assetPath, resourceType);
                }
            }

            EditorGUI.BeginChangeCheck();

            var newObj = EditorGUI.ObjectField(position, asset, resourceType, allowSceneObjects: false);

            if (!EditorGUI.EndChangeCheck())
                return false;

            if (newObj == null)
            {
                guid = default;
                return true;
            }

            // Disallow directly referencing a directory asset
            if (newObj is DefaultAsset)
            {
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(newObj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (UnityEditorHelper.IsResourcesPath(assetPath))
                {
                    guid = new SerializeGuid(AssetDatabase.AssetPathToGUID(assetPath));
                    return true;
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
            position = EditorGUI.PrefixLabel(position, label);

            var member = UnityEditorHelper.GetSerializedMember(property);

            if (member.MemberType != typeof(SerializeGuid) &&
                member.MemberType != typeof(SerializeGuid[]) &&
                member.MemberType != typeof(List<SerializeGuid>))
            {
                const string kText = "Incorrect member type";
                const string kTooltip = nameof(ResourceReferenceAttribute) + " should only be used with the " + nameof(SerializeGuid) + " type";

                ShowErrorContent(ref position, kTooltip);
                EditorGUI.LabelField(position, new GUIContent(kText, kTooltip), EditorStyles.boldLabel);
                return;
            }

            var optionsRect = new RectOffset((int)(position.width - position.height - Spacing), 0, 0, 0).Remove(position);
            position = position.PadRight(optionsRect.width + Spacing);

            var guid = SerializeGuid.EditorUtil.GetGuid(property);

            if (DrawAssetField(position, ref guid))
            {
                SerializeGuid.EditorUtil.SetGuid(property, guid);
            }

            if (EditorGUIEx.ThreeDotMenuButton(optionsRect))
            {
                ShowContextMenu(guid, property);
            }
        }
    }
}
