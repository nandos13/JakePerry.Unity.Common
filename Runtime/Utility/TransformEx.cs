using System;
using System.Text;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class TransformEx
    {
        public static void GetHierarchyPath(this Transform transform, StringBuilder sb, string delimeter = "/")
        {
            _ = sb ?? throw new ArgumentNullException(nameof(sb));

            var trs = transform;
            bool appendDelim = false;

            while (trs != null)
            {
                if (appendDelim)
                    sb.Append(delimeter);

                sb.Append(trs.name);

                trs = trs.parent;
                appendDelim = true;
            }
        }

        public static string GetHierarchyPath(this Transform transform, string delimeter = "/")
        {
            var sb = StringBuilderCache.Acquire();
            GetHierarchyPath(transform, sb, delimeter);

            return StringBuilderCache.GetStringAndRelease(sb); ;
        }
    }
}
