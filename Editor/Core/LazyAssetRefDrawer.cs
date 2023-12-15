using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(LazyAssetRef<>))]
    public sealed class LazyAssetRefDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private void Foo(SerializeGuid guid)
        {
            UnityEngine.Object resourceObj = null;

            if (LazyAssetRefEditorUtil.TryGetResourcePath(guid, out string resourcePath))
            {

            }

            /*
            var resourcePath = assetResourcePathProp.stringValue;
            if (!string.IsNullOrEmpty(resourcePath))
            {
                resourceObj = AssetProxy.FromResourcePath(resourcePath).EditorAsset;
            }

            EditorGUI.BeginChangeCheck();

            // TODO: Reword this.
            var content = new GUIContent(
                "Reference",
                "The asset referenced by the current resource path. Drag an asset here to automatically update the resource path.");

            var newObj = EditorGUI.ObjectField(layout.GetRect(), content, resourceObj, typeof(GameObject), allowSceneObjects: false);

            if (EditorGUI.EndChangeCheck())
            {
                if (newObj == null)
                {
                    assetResourcePathProp.stringValue = string.Empty;
                }
                else
                {
                    var assetPath = AssetDatabase.GetAssetPath(newObj);

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (TryGetResourcesPath(assetPath, out string resourceRelativePath))
                        {
                            assetResourcePathProp.stringValue = resourceRelativePath;
                        }
                        else
                            Debug.LogError("Error: Asset is not in a Resources folder.", newObj);
                    }
                    else
                        Debug.LogError("Error: Failed to get asset path for the selected asset.", newObj);
                }
            }

            EditorGUI.PropertyField(layout.GetRect(assetResourcePathProp), assetResourcePathProp);
            */
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProp = property.FindPropertyRelative("m_guid");

            //using (new EditorGUI.DisabledScope(true))
            //{
            //    SerializeGuidDrawer.DrawGUIWithoutOptions(position, guidProp, GUIContent.none);
            //}

            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label);

            if (SerializeGuid.TryDeserializeGuid(guidProp, out SerializeGuid guid))
            {
                // TODO...
                Foo(guid);
            }
            else
            {
                Debug.LogError("Failed to deserialize Guid.");
            }
        }
    }
}
