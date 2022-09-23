using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// TODO: Documentation...
    /// </summary>
    public sealed class EditorLayoutHelper
    {
        private readonly Vector2 m_initPoint;
        private readonly float m_width;

        private float m_offset;
        private float m_verticalSpacing;

        /// <summary>
        /// Returns the total height of all rects calculated by this object so far.
        /// </summary>
        public float TotalHeight => m_offset;

        public float VerticalSpacing
        {
            get => m_verticalSpacing;
            set
            {
                if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
                m_verticalSpacing = value;
            }
        }

        public EditorLayoutHelper(Vector2 point, float width)
        {
            m_initPoint = point;
            m_width = width;
            m_verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
        }

        public EditorLayoutHelper(float x, float y, float width)
            : this(new Vector2(x, y), width)
        { }

        public EditorLayoutHelper(Rect rect)
            : this(rect.position, rect.width)
        { }

        /// <summary>
        /// Simulates getting a rect with a given height.
        /// </summary>
        public void SimulateRect(float height)
        {
            var spacing = m_offset > 0 ? m_verticalSpacing : 0f;
            m_offset += spacing + height;
        }

        /// <summary>
        /// Simulates getting a rect with a height of <see cref="EditorGUIUtility.singleLineHeight"/>.
        /// </summary>
        public void SimulateRect()
        {
            SimulateRect(EditorGUIUtility.singleLineHeight);
        }

        /// <summary>
        /// Simulate getting a rect for a serialized property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="includeChildren"></param>
        public void SimulateRect(SerializedProperty property, bool includeChildren = false)
        {
            var height = EditorGUI.GetPropertyHeight(property, includeChildren);
            SimulateRect(height);
        }

        /// <summary>
        /// Calculates a rect with a given height.
        /// </summary>
        public Rect GetRect(float height)
        {
            var spacing = m_offset > 0 ? m_verticalSpacing : 0f;

            var rect = new Rect(
                m_initPoint.x,
                m_initPoint.y + spacing + m_offset,
                m_width,
                height);

            m_offset += spacing + height;

            return rect;
        }

        /// <summary>
        /// Calculates a rect with a height of <see cref="EditorGUIUtility.singleLineHeight"/>.
        /// </summary>
        public Rect GetRect()
        {
            return GetRect(EditorGUIUtility.singleLineHeight);
        }

        /// <summary>
        /// Calculates a rect for a serialized property.
        /// </summary>
        public Rect GetRect(SerializedProperty property, bool includeChildren = false)
        {
            var height = EditorGUI.GetPropertyHeight(property, includeChildren);
            return GetRect(height);
        }
    }
}
