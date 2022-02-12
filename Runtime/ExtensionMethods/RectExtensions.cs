using UnityEngine;

namespace JakePerry.Unity
{
    public static class RectExtensions
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

        public static Rect ResizeAroundCenter(this Rect r, Vector2 size)
        {
            return new Rect(r.center - size / 2f, size);
        }
    }
}
