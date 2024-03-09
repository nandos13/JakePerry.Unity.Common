using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JakePerry.Unity
{
    /// <summary>
    /// Provides methods for retrieving namespaces from assemblies
    /// loaded into the Unity domain.
    /// </summary>
    public static class NamespaceCache
    {
        private struct NamespaceData
        {
            public string thisNamespace;
            public string fullNamespace;
        }

        private static Namespace _root;
        private static bool _dirty = true;

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _dirty = true;
            _root = default;
        }

        private static bool TryFindMatch(List<NamespaceData> list, ReadOnlySpan<char> span, out NamespaceData match)
        {
            foreach (var n in list)
            {
                if (span.Equals(n.thisNamespace.AsSpan(), StringComparison.Ordinal))
                {
                    match = n;
                    return true;
                }
            }

            match = default;
            return false;
        }

        private static Namespace ConsolidateData(Dictionary<string, List<NamespaceData>> dict, NamespaceData data)
        {
            List<Namespace> children = null;
            if (dict.TryGetValue(data.fullNamespace, out var list) && list.Count > 0)
            {
                children = new List<Namespace>(capacity: list.Count);
                foreach (var c in list)
                {
                    if (dict.ContainsKey(c.fullNamespace))
                    {
                        children.Add(ConsolidateData(dict, c));
                    }
                    else
                    {
                        children.Add(new Namespace(c.thisNamespace, c.fullNamespace, Array.Empty<Namespace>()));
                    }
                }
            }

            return new Namespace(data.thisNamespace, data.fullNamespace, children?.ToArray() ?? Array.Empty<Namespace>());
        }

        private static int CompareData(NamespaceData x, NamespaceData y)
        {
            return StringComparer.Ordinal.Compare(x.thisNamespace, y.thisNamespace);
        }

        private static void BuildCacheIfRequired()
        {
            if (!_dirty) return;

            UnityEngine.Profiling.Profiler.BeginSample("NamespaceCache Build");

            var dict = new Dictionary<string, List<NamespaceData>>(StringComparer.Ordinal);

            var root = new NamespaceData { thisNamespace = string.Empty, fullNamespace = string.Empty };
            dict[string.Empty] = new List<NamespaceData>(capacity: 64);

            foreach (var t in TypeCache.GetTypesDerivedFrom(typeof(object)))
            {
                string @namespace = t.Namespace;

                if (string.IsNullOrEmpty(@namespace))
                {
                    continue;
                }

                if (dict.ContainsKey(@namespace))
                {
                    continue;
                }

                var node = root;

                int segmentStart = 0;
                while (segmentStart < @namespace.Length)
                {
                    int nextPeriodChar = @namespace.IndexOf('.', segmentStart);
                    int segmentEnd = (nextPeriodChar < 0) ? @namespace.Length : nextPeriodChar;

                    int segmentLength = segmentEnd - segmentStart;
                    var span = @namespace.AsSpan(segmentStart, segmentLength);

                    var list = dict[node.fullNamespace];

                    if (!TryFindMatch(list, span, out NamespaceData match))
                    {
                        string thisNamespace = span.ToString();
                        string fullNamespace;

                        if (node.fullNamespace.Length == 0)
                        {
                            fullNamespace = thisNamespace;
                        }
                        else if (nextPeriodChar > -1)
                        {
                            fullNamespace = @namespace.Substring(0, nextPeriodChar);
                        }
                        else
                        {
                            fullNamespace = @namespace;
                        }

                        match = new NamespaceData { thisNamespace = thisNamespace, fullNamespace = fullNamespace };
                        list.Add(match);

                        dict[fullNamespace] = new(capacity: 8);
                    }

                    node = match;

                    segmentStart = segmentEnd + 1;
                }
            }

            foreach (var l in dict.Values)
            {
                l.Sort(comparison: CompareData);
            }

            _root = ConsolidateData(dict, root);

            _dirty = false;

            // TODO: Remove this after some optimization
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public static Namespace GetGlobalNamespace()
        {
            BuildCacheIfRequired();
            return _root;
        }

        public static Namespace GetNamespace(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            BuildCacheIfRequired();

            string match = type.Namespace;
            if (string.IsNullOrEmpty(match))
            {
                return _root;
            }

            var node = _root;

            // TODO: This can be sped up using spans, just check name instead of fullname
        CHECK_NODE_CHILDREN:
            for (int i = node.NestedCount - 1; i >= 0; --i)
            {
                var child = node.GetNestedNamespace(i);
                if (match.StartsWith(child.FullName, StringComparison.Ordinal))
                {
                    node = child;
                    goto CHECK_NODE_CHILDREN;
                }
            }

            if (StringComparer.Ordinal.Equals(node.FullName, match))
            {
                return node;
            }

            throw new InvalidOperationException();
        }

        public static bool TryGetNamespace(string name, out Namespace @namespace)
        {
            BuildCacheIfRequired();

            @namespace = _root;

            if (string.IsNullOrEmpty(name))
            {
                @namespace = _root;
                return true;
            }

            int segmentStart = 0;
            while (segmentStart < name.Length)
            {
                int nextPeriodChar = name.IndexOf('.', segmentStart);
                int segmentEnd = (nextPeriodChar < 0) ? name.Length : nextPeriodChar;

                int segmentLength = segmentEnd - segmentStart;
                var span = name.AsSpan(segmentStart, segmentLength);

                if (!@namespace.TryGetNestedNamespace(span, out Namespace next))
                {
                    return false;
                }

                @namespace = next;
            }

            return true;
        }
    }
}
