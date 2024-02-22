using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class BoundsEx
    {
        /// <summary>
        /// Attempt to retrieve the local-space bounds of the mesh targeted by the given renderer.
        /// </summary>
        /// <param name="r">The target renderer.</param>
        /// <param name="b">The resulting local-space bounds.</param>
        public static bool TryGetMeshBounds(MeshRenderer r, out Bounds b)
        {
            if (r != null &&
                r.gameObject.TryGetComponent(out MeshFilter filter))
            {
                var mesh = filter.sharedMesh;
                if (mesh != null)
                {
                    b = mesh.bounds;
                    return true;
                }
            }

            b = default;
            return false;
        }

        private static Bounds GetBakedMeshBounds(SkinnedMeshRenderer r, ref Mesh copyMesh)
        {
            if (copyMesh is null) copyMesh = new Mesh();
            else copyMesh.Clear();

            r.BakeMesh(copyMesh, true);
            copyMesh.RecalculateBounds();

            return copyMesh.bounds;
        }

        /// <inheritdoc cref="TryGetMeshBounds(MeshRenderer, out Bounds)"/>
        /// <param name="bakeMesh">
        /// Specifies whether the skinned mesh renderer should be baked to a new mesh in order
        /// to obtain an accurate bounding box representation of its current animated state;
        /// Otherwise, the bounds is taken directly from the shared mesh and may not be accurate.
        /// <para>
        /// Note: Specifying <see langword="true"/> will result in a new mesh to be created. This
        /// will cause garbage allocation &amp; decreased performance.
        /// </para>
        /// </param>
        public static bool TryGetMeshBounds(SkinnedMeshRenderer r, bool bakeMesh, out Bounds b)
        {
            if (r != null)
            {
                if (bakeMesh)
                {
                    var copyMesh = new Mesh();
                    try
                    {
                        b = GetBakedMeshBounds(r, ref copyMesh);
                        return true;
                    }
                    finally { UnityEngine.Object.DestroyImmediate(copyMesh); }
                }
                else
                {
                    var mesh = r.sharedMesh;
                    if (mesh != null)
                    {
                        b = mesh.bounds;
                        return true;
                    }
                }
            }

            b = default;
            return false;
        }

        /// <summary>
        /// Calculate a local bounding volume encompassing all rendered meshes under <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root GameObject.</param>
        /// <param name="includeInactive">
        /// Specifies whether inactive mesh renderers should be included.
        /// </param>
        /// <param name="bakeSkinnedRenderers">
        /// Specifies whether skinned mesh renderers should be baked.
        /// See <see cref="TryGetMeshBounds(SkinnedMeshRenderer, bool, out Bounds)"/> for more info.
        /// </param>
        public static Bounds CalculateLocalBoundingVolume(GameObject root, bool includeInactive = false, bool bakeSkinnedRenderers = false)
        {
            bool set = false;
            Bounds b = default;

            var worldToRootLocal = root.transform.worldToLocalMatrix;

            Mesh copyMesh = null;

            using var scope = ListPool.RentInScope(out List<Renderer> renderers);
            try
            {
                root.GetComponentsInChildren(includeInactive, renderers);

                foreach (var r in renderers)
                {
                    Bounds rendererBounds;

                    if (r is MeshRenderer mr)
                    {
                        if (!TryGetMeshBounds(mr, out rendererBounds))
                            continue;
                    }
                    else if (r is SkinnedMeshRenderer smr)
                    {
                        if (bakeSkinnedRenderers)
                        {
                            rendererBounds = GetBakedMeshBounds(smr, ref copyMesh);
                        }
                        else
                        {
                            var mesh = smr.sharedMesh;
                            if (mesh == null)
                                continue;

                            rendererBounds = mesh.bounds;
                        }
                    }
                    else
                        continue;

                    if (rendererBounds.size.sqrMagnitude == 0f)
                        continue;

                    var trs = r.transform;
                    var localToRootLocal = worldToRootLocal * trs.localToWorldMatrix;

                    var unalignedBox = new BoxUnaligned3D((Box3D)rendererBounds, localToRootLocal);

                    if (AssignValueUtility.SetValueType(ref set, true))
                    {
                        b = unalignedBox.GetWorldBoundingBox();
                    }
                    else
                    {
                        foreach (var cornerRootLocal in unalignedBox.EnumerateCorners())
                            b.Encapsulate(cornerRootLocal);
                    }
                }
            }
            finally
            {
                if (copyMesh != null)
                    UnityEngine.Object.DestroyImmediate(copyMesh);
            }

            return b;
        }
    }
}
