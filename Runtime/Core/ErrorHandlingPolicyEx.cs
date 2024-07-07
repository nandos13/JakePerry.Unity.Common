using System;

namespace JakePerry.Unity
{
    /// <summary>
    /// Contains helper methods for the <see cref="ErrorHandlingPolicy"/> enum type.
    /// </summary>
    public static class ErrorHandlingPolicyEx
    {
        /// <summary>
        /// Use the current policy enum value, or a fallback value if the current
        /// value is equal to <see cref="ErrorHandlingPolicy.Default"/>.
        /// </summary>
        /// <param name="fallback">
        /// The fallback policy value.
        /// </param>
        public static ErrorHandlingPolicy OrFallback(this ErrorHandlingPolicy o, ErrorHandlingPolicy fallback)
        {
            return (o == ErrorHandlingPolicy.Default) ? fallback : o;
        }

        /// <summary>
        /// Cast the current policy enum value to a byte.
        /// </summary>
        public static byte ToByte(this ErrorHandlingPolicy value)
        {
            if (value < ErrorHandlingPolicy.Default ||
                value > ErrorHandlingPolicy.ThrowException)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return (byte)(int)value;
        }
    }
}
