using JakePerry.Threading;
using System.Collections.Generic;

namespace JakePerry.Unity
{
    /// <summary>
    /// Provides thread-safe utility methods to get the short name or the
    /// base namespace of a given namespace string.
    /// </summary>
    public static class NamespaceUtil
    {
        private static readonly Dictionary<string, string> _baseCache = new();
        private static readonly Dictionary<string, string> _nameCache = new();

        private static SpinLockSlim _lock = SpinLockSlim.Create();

        /// <summary>
        /// Get the base namespace for <paramref name="namespc"/>.
        /// <para/>
        /// ie. 'System' is the base namespace of 'System.Text'.
        /// </summary>
        public static string GetBaseNamespace(string namespc)
        {
            if (string.IsNullOrEmpty(namespc)) return string.Empty;

            string @base;
            bool lockWasTaken = false;
            try
            {
                _lock.AcquireLock(ref lockWasTaken);

                if (!_baseCache.TryGetValue(namespc, out @base))
                {
                    // If no period char is found, this is a base namespace ie. 'System'.
                    int i = namespc.LastIndexOf('.');
                    @base = i < 0 ? string.Empty : namespc.Substring(0, i);

                    _baseCache[namespc] = @base;
                }
            }
            finally { _lock.ReleaseLock(lockWasTaken); }

            return @base;
        }

        /// <summary>
        /// Get the short name for <paramref name="namespc"/>.
        /// <para/>
        /// ie. 'Text' is the short name of 'System.Text'.
        /// </summary>
        public static string GetShortName(string namespc)
        {
            if (string.IsNullOrEmpty(namespc)) return string.Empty;

            string name;
            bool lockWasTaken = false;
            try
            {
                _lock.AcquireLock(ref lockWasTaken);

                if (!_nameCache.TryGetValue(namespc, out name))
                {
                    // If no period char is found, this is a base namespace ie. 'System'.
                    int i = namespc.LastIndexOf('.');
                    name = i < 0 ? namespc : namespc.Substring(i + 1);

                    _nameCache[namespc] = name;
                }
            }
            finally { _lock.ReleaseLock(lockWasTaken); }

            return name;
        }
    }
}
