using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    internal static class EditorHelpersStatic
    {
        private static readonly GUIContent _tempContent = new();

        internal static float LineHeight => EditorGUIUtility.singleLineHeight;

        internal static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Get a shared <see cref="GUIContent"/> instance.
        /// </summary>
        /// <remarks>
        /// Great for preventing allocations when the content is only used
        /// immediately and not cached (ie. drawing a label).
        /// </remarks>
        /// <returns>
        /// The shared <see cref="GUIContent"/> instance.
        /// </returns>
        internal static GUIContent TempContent
        {
            get
            {
                _tempContent.text = string.Empty;
                _tempContent.tooltip = string.Empty;
                _tempContent.image = null;
                return _tempContent;
            }
        }

        /// <summary>
        /// Get a shared <see cref="GUIContent"/> instance &amp; set the text.
        /// </summary>
        /// <inheritdoc cref="TempContent"/>
        internal static GUIContent GetTempContent(string text)
        {
            _tempContent.text = text;
            _tempContent.tooltip = string.Empty;
            _tempContent.image = null;
            return _tempContent;
        }

        /// <summary>
        /// Get a shared <see cref="GUIContent"/> instance &amp; set the text and tooltip.
        /// </summary>
        /// <inheritdoc cref="TempContent"/>
        internal static GUIContent GetTempContent(string text, string tooltip)
        {
            _tempContent.text = text;
            _tempContent.tooltip = tooltip;
            _tempContent.image = null;
            return _tempContent;
        }

        /// <summary>
        /// Get a shared <see cref="GUIContent"/> instance &amp; set the texture.
        /// </summary>
        /// <inheritdoc cref="TempContent"/>
        internal static GUIContent GetTempContent(Texture image)
        {
            _tempContent.text = string.Empty;
            _tempContent.tooltip = string.Empty;
            _tempContent.image = image;
            return _tempContent;
        }
    }
}
