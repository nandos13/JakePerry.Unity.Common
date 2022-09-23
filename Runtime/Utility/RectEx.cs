using System;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class RectEx
    {
        public static Rect Pad(this Rect r, float padding)
        {
            float padding2 = padding * 2;

            return new Rect(
                r.x + padding,
                r.y + padding,
                r.width - padding2,
                r.height - padding2);
        }

        public static Rect PadLeft(this Rect r, float padding)
        {
            return new Rect(r.x + padding, r.y, r.width - padding, r.height);
        }

        public static Rect PadRight(this Rect r, float padding)
        {
            return new Rect(r.x, r.y, r.width - padding, r.height);
        }

        public static Rect PadTop(this Rect r, float padding)
        {
            return new Rect(r.x, r.y + padding, r.width, r.height - padding);
        }

        public static Rect PadBottom(this Rect r, float padding)
        {
            return new Rect(r.x, r.y, r.width, r.height - padding);
        }

        public static Rect WithWidth(this Rect r, float width)
        {
            r.width = width;
            return r;
        }

        public static Rect WithHeight(this Rect r, float height)
        {
            r.height = height;
            return r;
        }

        public static Rect ResizeAroundCenter(this Rect r, Vector2 size)
        {
            return new Rect(r.center - size / 2f, size);
        }

        /// <summary>
        /// Slice the current <see cref="Rect"/> in two along the x-axis.
        /// </summary>
        /// <param name="source">The current rect.</param>
        /// <param name="amount">Indicates how far along the width to slice the rect. Range [0,1].</param>
        /// <param name="left">The resulting left rect.</param>
        /// <param name="right">The resulting right rect.</param>
        public static void SliceX(this Rect source, float amount, out Rect left, out Rect right)
        {
            amount = Mathf.Clamp01(amount);

            left = right = source;
            left.width *= amount;

            right.x += left.width;
            right.width -= left.width;
        }

        /// <summary>
        /// Slice the current <see cref="Rect"/> in two along the y-axis.
        /// </summary>
        /// <inheritdoc cref="SliceX(Rect, float, out Rect, out Rect)"/>
        /// <param name="top">The resulting top rect.</param>
        /// <param name="bottom">The resulting bottom rect.</param>
        public static void SliceY(this Rect source, float amount, out Rect top, out Rect bottom)
        {
            amount = Mathf.Clamp01(amount);

            top = bottom = source;
            top.height *= amount;

            bottom.y += top.height;
            bottom.height -= top.height;
        }

        private static void Slice(this Rect source, int axis, int count, List<Rect> output, float spacing)
        {
            _ = output ?? throw new ArgumentNullException(nameof(output));

            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            axis = axis % 2;

            spacing = Mathf.Max(spacing, 0f);

            int spaceCount = count - 1;
            float totalSpacing = spaceCount * spacing;

            float totalRectsSize = Mathf.Max(source.size[axis] - totalSpacing, 0f);

            Vector2 singleRectSize = source.size;
            singleRectSize[axis] = totalRectsSize / count;

            for (int i = 0; i < count; i++)
            {
                var pos = source.position;
                pos[axis] += (singleRectSize[axis] + spacing) * i;

                Rect r = new Rect(pos, singleRectSize);

                if (output.Count > i) output.Insert(i, r);
                else output.Add(r);
            }
        }

        public static void SliceX(this Rect source, int count, List<Rect> output, float spacing)
        {
            Slice(source, 0, count, output, spacing);
        }

        public static void SliceY(this Rect source, int count, List<Rect> output, float spacing)
        {
            Slice(source, 1, count, output, spacing);
        }
    }
}
