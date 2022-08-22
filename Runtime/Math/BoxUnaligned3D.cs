using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public readonly struct BoxUnaligned3D : IEquatable<BoxUnaligned3D>
    {
        private readonly Box3D m_alignedBox;
        private readonly Matrix4x4 m_trsMatrix;

        public Box3D AlignedBox => m_alignedBox;

        public Matrix4x4 TRS => m_trsMatrix;

        public Vector3 Center => m_trsMatrix.MultiplyPoint3x4(m_alignedBox.Center);

        public BoxUnaligned3D(Box3D box, Matrix4x4 matrix)
        {
            m_alignedBox = box;
            m_trsMatrix = matrix;
        }

        public BoxUnaligned3D(Box3D box, Vector3 translation, Quaternion rotation, Vector3 scale)
            : this(box, Matrix4x4.TRS(translation, rotation, scale))
        { }

        public Vector3 ClosestPoint(Vector3 point)
        {
            var localPoint = m_trsMatrix.inverse.MultiplyPoint3x4(point);

            var closestPointLocal = m_alignedBox.ClosestPoint(localPoint);

            return m_trsMatrix.MultiplyPoint3x4(closestPointLocal);
        }

        public bool Contains(Vector3 point)
        {
            var localPoint = m_trsMatrix.inverse.MultiplyPoint3x4(point);

            return m_alignedBox.Contains(localPoint);
        }

        public float SqrDistance(Vector3 point)
        {
            var closestPoint = ClosestPoint(point);
            return Vector3.SqrMagnitude(point - closestPoint);
        }

        public Bounds GetWorldBoundingBox()
        {
            var bounds = new Bounds(Center, Vector3.zero);

            foreach (var c in EnumerateCorners())
                bounds.Encapsulate(c);

            return bounds;
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

            var localNormal = Vector3Ex.UnitVec(axis) * sign;
            var worldNormal = m_trsMatrix.MultiplyPoint3x4(localNormal).normalized;

            var localFacePoint = m_alignedBox.Center + (localNormal * m_alignedBox.Extents[axis]);
            var worldFacePoint = m_trsMatrix.MultiplyPoint3x4(localFacePoint);

            return new Plane(worldNormal, worldFacePoint);
        }

        public bool Equals(BoxUnaligned3D other)
        {
            return m_alignedBox.Equals(other.m_alignedBox) && m_trsMatrix.Equals(other.m_trsMatrix);
        }

        public override bool Equals(object obj)
        {
            return obj is BoxUnaligned3D other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 29;
            hash = hash * 31 + m_alignedBox.GetHashCode();
            hash = hash * 31 + m_trsMatrix.GetHashCode();
            return hash;
        }

        public struct CornerEnumerator : IEnumerator<Vector3>
        {
            public readonly Matrix4x4 matrix;

            private Box3D.CornerEnumerator m_enumerator;

            public Vector3 Current => matrix.MultiplyPoint3x4(m_enumerator.Current);
            object IEnumerator.Current => (object)this.Current;

            public BoxUnaligned3D Box => new BoxUnaligned3D(m_enumerator.Box, matrix);

            public CornerEnumerator(BoxUnaligned3D box) { matrix = box.TRS; m_enumerator = new Box3D.CornerEnumerator(box.AlignedBox); }

            /// <inheritdoc cref="Box3D.CornerEnumerator.GetCorner(int)"/>
            internal Vector3 GetCorner(int index) => m_enumerator.GetCorner(index);

            public bool MoveNext() => m_enumerator.MoveNext();
            public void Reset() => m_enumerator.Reset();
            public void Dispose() => m_enumerator.Dispose();
        }

        public readonly struct CornerEnumerable : IEnumerable<Vector3>
        {
            public readonly BoxUnaligned3D box;
            public CornerEnumerable(BoxUnaligned3D b) { box = b; }
            public CornerEnumerator GetEnumerator() => new CornerEnumerator(box);
            IEnumerator<Vector3> IEnumerable<Vector3>.GetEnumerator() => this.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public bool Equals(CornerEnumerable other) => box.Equals(other.box);
            public override bool Equals(object obj) => obj is CornerEnumerable other && this.Equals(other);
            public override int GetHashCode() => box.GetHashCode();

            public static bool operator ==(CornerEnumerable x, CornerEnumerable y) => x.Equals(y);
            public static bool operator !=(CornerEnumerable x, CornerEnumerable y) => !x.Equals(y);
        }

        public static bool operator ==(BoxUnaligned3D x, BoxUnaligned3D y) => x.Equals(y);
        public static bool operator !=(BoxUnaligned3D x, BoxUnaligned3D y) => !x.Equals(y);
    }
}
