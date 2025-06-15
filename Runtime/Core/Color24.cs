using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Representation of RGB colors in 24 bit format.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Color24 : IFormattable
    {
        [FieldOffset(0)]
        private readonly ushort rg;

        /// <summary>
        /// Red component of the color.
        /// </summary>
        [FieldOffset(0)]
        public byte r;

        /// <summary>
        /// Green component of the color.
        /// </summary>
        [FieldOffset(1)]
        public byte g;

        /// <summary>
        /// Blue component of the color.
        /// </summary>
        [FieldOffset(2)]
        public byte b;

        /// <summary>
        /// RGB = (0, 0, 0)
        /// </summary>
        public static Color24 Black
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0);
        }

        /// <summary>
        /// RGB = (255, 255, 255)
        /// </summary>
        public static Color24 White
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255);
        }

        /// <summary>
        /// RGB = (127, 127, 127)
        /// </summary>
        public static Color24 Grey
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(127);
        }

        /// <summary>
        /// RGB = (255, 0, 0)
        /// </summary>
        public static Color24 Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255, 0, 0);
        }

        /// <summary>
        /// RGB = (0, 255, 0)
        /// </summary>
        public static Color24 Green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 255, 0);
        }

        /// <summary>
        /// RGB = (0, 0, 255)
        /// </summary>
        public static Color24 Blue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 0, 255);
        }

        /// <summary>
        /// RGB = (255, 255, 0)
        /// </summary>
        public static Color24 Yellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255, 255, 0);
        }

        /// <summary>
        /// RGB = (255, 0, 255)
        /// </summary>
        public static Color24 Magenta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(255, 0, 255);
        }

        /// <summary>
        /// RGB = (0, 255, 255)
        /// </summary>
        public static Color24 Cyan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 255, 255);
        }

        public byte this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => r,
                    1 => g,
                    2 => b,
                    _ => throw new IndexOutOfRangeException($"Invalid Color24 index ({index})!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    default:
                        throw new IndexOutOfRangeException($"Invalid Color24 index ({index})!");
                }
            }
        }

        public Color24(byte uniform)
        {
            rg = 0;
            r = g = b = uniform;
        }

        public Color24(byte r, byte g, byte b)
        {
            rg = 0;
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public readonly bool Equals(Color24 c)
        {
            return rg == c.rg && b == c.b;
        }

        public readonly override bool Equals(object obj)
        {
            if (obj is Color24 c24) return this.Equals(c24);
            if (obj is Color32 c32) return this.Equals((Color24)c32);
            return false;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(rg, b);
        }

        public readonly Color32 ToColor32(byte alpha = 255)
        {
            return new Color32(r, g, b, alpha);
        }

        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            formatProvider ??= CultureInfo.InvariantCulture.NumberFormat;

            var sb = StringBuilderCache.Acquire();
            sb.Append("RGB(");
            sb.Append(r.ToString(format, formatProvider));
            sb.Append(", ");
            sb.Append(g.ToString(format, formatProvider));
            sb.Append(", ");
            sb.Append(b.ToString(format, formatProvider));
            sb.Append(")");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public readonly string ToString(string format)
        {
            return ToString(format, null);
        }

        public readonly override string ToString()
        {
            return ToString(null, null);
        }

        public static implicit operator Color32(Color24 c24)
        {
            return new Color32(c24.r, c24.g, c24.b, 255);
        }

        public static explicit operator Color24(Color32 c32)
        {
            return new Color24(c32.r, c32.g, c32.b);
        }

        public static implicit operator Color(Color24 c24)
        {
            var c32 = (Color32)c24;
            return (Color)c32;
        }

        public static explicit operator Color24(Color c)
        {
            var c32 = (Color32)c;
            return (Color24)c32;
        }
    }
}
