using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public readonly struct Box3D : IEquatable<Box3D>
    {
        private readonly Vector3 m_center;
        private readonly Vector3 m_extents;

        public Vector3 Center => m_center;

        public Vector3 Extents => m_extents;

        public Vector3 Size => m_extents * 2f;

        public Vector3 Min => m_center - m_extents;

        public Vector3 Max => m_center + m_extents;

        public Box3D(Vector3 center, Vector3 extents)
        {
            m_center = center;
            m_extents = Vector3Ex.Abs(extents);
        }

        public Vector3 ClosestPoint(Vector3 point)
        {
            var p = point;

            for (int i = 0; i < 3; i++)
            {
                var delta = point[i] - m_center[i];
                if (Mathf.Abs(delta) > m_extents[i])
                    p[i] = m_center[i] + (m_extents[i] * Mathf.Sign(delta));
            }

            return p;
        }

        public bool Contains(Vector3 point)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(point[i] - m_center[i]) > m_extents[i])
                    return false;
            }

            return true;
        }

        public Box3D Encapsulate(Vector3 point)
        {
            var min = Vector3.Min(Min, point);
            var max = Vector3.Max(Max, point);

            return Box3D.MinMax(min, max);
        }

        public Box3D Encapsulate(Box3D other)
        {
            var min = Vector3.Min(Min, other.Min);
            var max = Vector3.Max(Max, other.Max);

            return Box3D.MinMax(min, max);
        }

        public Box3D Expanded(Vector3 amount)
        {
            return new Box3D(m_center, m_extents + amount * 0.5f);
        }

        public Box3D Expanded(float amount)
        {
            return Expanded(Vector3Ex.Scalar(amount));
        }

        public bool Intersects(Box3D other)
        {
            for (int i = 0; i < 3; i++)
            {
                var centerDist = Mathf.Abs(other.m_center[i] - m_center[i]);

                if (centerDist > m_extents[i])
                {
                    centerDist -= m_extents[i];
                    if (centerDist > other.m_extents[i])
                        return false;
                }
            }

            return true;
        }

        public bool Intersects(Ray ray, out float distance)
        {
            // Iterate all axes
            for (int i = 0; i < 3; i++)
            {
                // Find face on axis for which the normal is opposite the ray's direction
                var axisDot = Vector3.Dot(ray.direction, Vector3Ex.UnitVec(i));

                if (axisDot == 0f)
                    continue;

                bool negative = axisDot > 0;
                var plane = GetFacePlane(i, negative);

                if (plane.GetDistanceToPoint(ray.origin) == 0f)
                {
                    distance = 0f;
                    return true;
                }

                float sign = plane.GetSide(ray.origin) ? 1f : -1f;
                var ray2 = new Ray(ray.origin, ray.direction * sign);

                if (plane.Raycast(ray2, out float enter))
                {
                    // Determine if the point is actually on the face by checking if it's within the extents
                    var planePoint = ray2.GetPoint(enter);
                    for (int j = 0; j < 3; j++)
                    {
                        if (i == j) continue;

                        var distToCenter = Mathf.Abs(Vector3.Dot(planePoint, Vector3Ex.UnitVec(j)) - m_center[j]);

                        if (distToCenter > m_extents[j])
                            goto NOT_ON_FACE;
                    }

                    // If no axes failed the checks above, we've successfully found an intersect point
                    distance = enter * sign;
                    return true;

                NOT_ON_FACE:
                    continue;
                }
            }

            distance = default;
            return false;
        }

        public float SqrDistance(Vector3 point)
        {
            float sqrDist = 0f;

            for (int i = 0; i < 3; i++)
            {
                var delta = point[i] - m_center[i];
                if (Mathf.Abs(delta) > m_extents[i])
                    sqrDist += Mathf.Abs(m_center[i] + (m_extents[i] * Mathf.Sign(delta)) - point[i]);
            }

            return sqrDist;
        }

        public CornerEnumerable EnumerateCorners()
        {
            return new CornerEnumerable(this);
        }

        public void GetCorners(
            out Vector3 c0,
            out Vector3 c1,
            out Vector3 c2,
            out Vector3 c3,
            out Vector3 c4,
            out Vector3 c5,
            out Vector3 c6,
            out Vector3 c7)
        {
            var e = new CornerEnumerator(this);
            c0 = e.GetCorner(0);
            c1 = e.GetCorner(1);
            c2 = e.GetCorner(2);
            c3 = e.GetCorner(3);
            c4 = e.GetCorner(4);
            c5 = e.GetCorner(5);
            c6 = e.GetCorner(6);
            c7 = e.GetCorner(7);
        }

        public Plane GetFacePlane(int axis, bool negative)
        {
            MathEx.PosMod(ref axis, 3);

            float sign = negative ? -1 : 1;

            var normal = Vector3Ex.UnitVec(axis) * sign;
            float distance = m_center[axis] - m_extents[axis];

            return new Plane(normal, distance);
        }

        public bool Equals(Box3D other)
        {
            return m_center.Equals(other.m_center) && m_extents.Equals(other.m_extents);
        }

        public override bool Equals(object obj)
        {
            return obj is Box3D other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 29;
            hash = hash * 31 + m_center.GetHashCode();
            hash = hash * 31 + m_extents.GetHashCode();
            return hash;
        }

        public static void Encapsulate(ref Box3D box, Vector3 point)
        {
            box = box.Encapsulate(point);
        }

        public static void Encapsulate(ref Box3D box, Box3D other)
        {
            box = box.Encapsulate(other);
        }

        public static void Expand(ref Box3D box, Vector3 amount)
        {
            box = box.Expanded(amount);
        }

        public static void Expand(ref Box3D box, float amount)
        {
            box = box.Expanded(Vector3Ex.Scalar(amount));
        }

        public static Box3D MinMax(Vector3 min, Vector3 max)
        {
            Vector3Ex.MinMax(ref min, ref max);

            var size = max - min;
            var extents = size / 2f;
            var center = min + size;

            return new Box3D(center, extents);
        }

        public struct CornerEnumerator : IEnumerator<Vector3>
        {
            public readonly Vector3 min, max;

            private int m_index;
            private Vector3 m_current;

            public Vector3 Current => m_current;
            object IEnumerator.Current => (object)this.Current;

            public Box3D Box => Box3D.MinMax(min, max);

            public CornerEnumerator(Box3D box)
            {
                min = box.Min;
                max = box.Max;
                m_index = default;
                m_current = default;
            }

            /// <summary>
            /// Get the position of a corner by index.
            /// </summary>
            /// <param name="index">Index in range [0,7] (inclusive).</param>
            internal Vector3 GetCorner(int index)
            {
                // This diagram describes corner order:
                //
                //        Y
                //   Z    |               5 +---------+. 7
                //    `.  |                 |`.       | `.
                //      `.|                 | 4`+-----+---+ 6
                //        +--------X        |   |     |   |
                //                        1 +---+-----+ 3 |
                //                           `. |      `. |
                //                             `+---------+
                //                              0         2
                //

                float x = (index / 2) % 2 == 0 ? min.x : max.x;
                float y = index < 4 ? min.y : max.y;
                float z = index % 2 == 0 ? min.z : max.z;
                return new Vector3(x, y, z);
            }

            public bool MoveNext()
            {
                if (m_index < 8)
                {
                    m_current = GetCorner(m_index);
                    ++m_index;
                    return true;
                }

                m_index = 8;
                m_current = default;
                return false;
            }

            public void Reset() { m_index = 0; }
            public void Dispose() { }
        }

        public readonly struct CornerEnumerable : IEnumerable<Vector3>, IEquatable<CornerEnumerable>
        {
            public readonly Box3D box;
            public CornerEnumerable(Box3D b) { box = b; }

            public CornerEnumerator GetEnumerator() => new CornerEnumerator(box);
            IEnumerator<Vector3> IEnumerable<Vector3>.GetEnumerator() => this.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public bool Equals(CornerEnumerable other) => box.Equals(other.box);
            public override bool Equals(object obj) => obj is CornerEnumerable other && this.Equals(other);
            public override int GetHashCode() => box.GetHashCode();

            public static bool operator ==(CornerEnumerable x, CornerEnumerable y) => x.Equals(y);
            public static bool operator !=(CornerEnumerable x, CornerEnumerable y) => !x.Equals(y);
        }

        public static bool operator ==(Box3D x, Box3D y) => x.Equals(y);
        public static bool operator !=(Box3D x, Box3D y) => !x.Equals(y);

        public static implicit operator Bounds(Box3D box)
        {
            return new Bounds(box.m_center, box.m_extents * 2f);
        }

        public static implicit operator Box3D(Bounds bounds)
        {
            return new Box3D(bounds.center, bounds.extents);
        }
    }
}
