using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshProcess
{
    public class VHACD : MonoBehaviour
    {
        // Should match `enum class FillMode` in VHACD.h
        [Serializable]
        public enum FillMode
        {
            FLOOD_FILL,    // 0
            SURFACE_ONLY,  // 1
            RAYCAST_FILL   // 2
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Parameters
        {
            public void Init()
            {
                m_callback = null;
                m_logger = null;
                m_taskRunner = null;
                m_maxConvexHulls = 32;
                m_resolution = 100000;
                m_minimumVolumePercentErrorAllowed = 1;
                m_maxRecursionDepth = 10;
                m_shrinkWrap = true;
                m_fillMode = FillMode.FLOOD_FILL;
                m_maxNumVerticesPerCH = 64;
                m_asyncACD = true;
                m_minEdgeLength = 2;
                m_findBestPlane = false;
            }

            public void* m_callback;
            public void* m_logger;
            public void* m_taskRunner;

            [Tooltip("The maximum number of convex hulls to produce. Performance sensitive: adding more MeshColliders slows down Unity at runtime.")]
            [Range(1, 2048)]
            public uint m_maxConvexHulls;

            [Tooltip("Maximum number of voxels generated during the voxelization stage. Higher value increases generation time.")]
            [Range(10000, 64000000)]
            public uint m_resolution;

            [Tooltip(
                "If the voxels are within X% of the volume of the hull, we consider this a close enough approximation.")]
            [Range(0.001f, 10)]
            public double m_minimumVolumePercentErrorAllowed;

            [Tooltip("Maximum recursion depth. Default value is 10.")]
            [Range(1, 15)]
            public uint m_maxRecursionDepth;

            [Tooltip(
                "This will project the output convex hull vertices onto the original source mesh to increase the floating point accuracy of the results. Default is true.")]
            public bool m_shrinkWrap;

            [Tooltip("How to fill the interior of the voxelized mesh")]
            public FillMode m_fillMode;

            [Tooltip("Controls the maximum number of triangles per convex-hull")]
            [Range(4, 1024)]
            public uint m_maxNumVerticesPerCH;

            [Tooltip("Whether or not to run asynchronously, taking advantage of additional cores")]
            public bool m_asyncACD;

            [Tooltip("Minimum size of a voxel edge. Default value is 2 voxels.")]
            [Range(1, 16)]
            public uint m_minEdgeLength;

            [Tooltip("If false, splits hulls in the middle. If true, tries to find optimal split plane location. False by default.")]
            public bool m_findBestPlane;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct ConvexHull
        {
            public double* m_points;
            public uint* m_triangles;
            public uint m_nPoints;
            public uint m_nTriangles;
        };

        [DllImport("libvhacd")] static extern unsafe void* CreateVHACD();

        [DllImport("libvhacd")] static extern unsafe void DestroyVHACD(void* pVHACD);

        [DllImport("libvhacd")]
        static extern unsafe bool ComputeFloat(
            void* pVHACD,
            float* points,
            uint countPoints,
            uint* triangles,
            uint countTriangles,
            Parameters* parameters);

        [DllImport("libvhacd")]
        static extern unsafe bool ComputeDouble(
            void* pVHACD,
            double* points,
            uint countPoints,
            uint* triangles,
            uint countTriangles,
            Parameters* parameters);

        [DllImport("libvhacd")] static extern unsafe uint GetNConvexHulls(void* pVHACD);

        [DllImport("libvhacd")]
        static extern unsafe void GetConvexHull(
            void* pVHACD,
            uint index,
            ConvexHull* convexHull);

        [DllImport("libvhacd")]
        static extern unsafe void FreeConvexHull(
            ConvexHull* convexHull);

        public Parameters m_parameters;

        public VHACD() { m_parameters.Init(); }

        [ContextMenu("Generate Convex Meshes")]
        public unsafe List<Mesh> GenerateConvexMeshes(Mesh mesh = null)
        {
            if (mesh == null)
            {
                mesh = GetComponent<MeshFilter>().sharedMesh;
            }
            var vhacd = CreateVHACD();
            var parameters = m_parameters;

            var verts = mesh.vertices;
            var tris = mesh.triangles;
            fixed (Vector3* pVerts = verts)
            fixed (int* pTris = tris)
            {
                ComputeFloat(
                    vhacd,
                    (float*)pVerts, (uint)verts.Length,
                    (uint*)pTris, (uint)tris.Length / 3,
                    &parameters);
            }

            var numHulls = GetNConvexHulls(vhacd);
            List<Mesh> convexMesh = new List<Mesh>((int)numHulls);
            foreach (var index in Enumerable.Range(0, (int)numHulls))
            {
                ConvexHull hull;
                GetConvexHull(vhacd, (uint)index, &hull);

                var hullMesh = new Mesh();
                var hullVerts = new Vector3[hull.m_nPoints];
                fixed (Vector3* pHullVerts = hullVerts)
                {
                    var pComponents = hull.m_points;
                    var pVerts = pHullVerts;

                    for (var pointCount = hull.m_nPoints; pointCount != 0; --pointCount)
                    {
                        pVerts->x = (float)pComponents[0];
                        pVerts->y = (float)pComponents[1];
                        pVerts->z = (float)pComponents[2];

                        pVerts += 1;
                        pComponents += 3;
                    }
                }

                hullMesh.SetVertices(hullVerts);

                var indices = new int[hull.m_nTriangles * 3];
                Marshal.Copy((System.IntPtr)hull.m_triangles, indices, 0, indices.Length);
                hullMesh.SetTriangles(indices, 0);

                
                convexMesh.Add(hullMesh);

                FreeConvexHull(&hull);
            }

            DestroyVHACD(vhacd);
            return convexMesh;
        }
    }
}
