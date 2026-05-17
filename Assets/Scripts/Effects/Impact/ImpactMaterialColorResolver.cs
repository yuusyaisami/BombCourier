using System.Collections.Generic;
using UnityEngine;

namespace BC.Effects.Impact
{
    public static class ImpactMaterialColorResolver
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Dictionary<Mesh, int[]> SubMeshLookupCache = new();

        public static Color ResolveColor(in ImpactEffectRequest request)
        {
            return ResolveColor(request.SurfaceCollider, request.Point, request.Normal, request.DefaultColor);
        }

        public static Color ResolveColor(RaycastHit hit, Color defaultColor)
        {
            if (hit.collider == null)
                return defaultColor;

            if (TryResolveMaterial(hit.collider, hit.triangleIndex, out Material material) && TryGetMaterialColor(material, out Color color))
                return color;

            return ResolveColor(hit.collider, hit.point, hit.normal, defaultColor);
        }

        public static Color ResolveColor(Collider surfaceCollider, Vector3 point, Vector3 normal, Color defaultColor)
        {
            if (surfaceCollider == null)
                return defaultColor;

            int triangleIndex = ResolveTriangleIndex(surfaceCollider, point, normal);

            if (TryResolveMaterial(surfaceCollider, triangleIndex, out Material material) && TryGetMaterialColor(material, out Color color))
                return color;

            return defaultColor;
        }

        private static int ResolveTriangleIndex(Collider surfaceCollider, Vector3 point, Vector3 normal)
        {
            if (surfaceCollider is not MeshCollider meshCollider || meshCollider.sharedMesh == null)
                return -1;

            Vector3 direction = normal.sqrMagnitude > 0.0001f ? -normal.normalized : Vector3.down;
            Vector3 origin = point - direction * 0.03f;

            return surfaceCollider.Raycast(new Ray(origin, direction), out RaycastHit hit, 0.12f)
                ? hit.triangleIndex
                : -1;
        }

        private static bool TryResolveMaterial(Collider surfaceCollider, int triangleIndex, out Material material)
        {
            material = null;

            Renderer targetRenderer = ResolveRenderer(surfaceCollider);
            if (targetRenderer == null)
                return false;

            Material[] materials = targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;

            int materialIndex = ResolveMaterialIndex(surfaceCollider, triangleIndex, materials.Length);
            material = materials[Mathf.Clamp(materialIndex, 0, materials.Length - 1)];
            return material != null;
        }

        private static Renderer ResolveRenderer(Collider surfaceCollider)
        {
            Renderer renderer = surfaceCollider.GetComponent<Renderer>();
            return renderer != null ? renderer : surfaceCollider.GetComponentInParent<Renderer>();
        }

        private static int ResolveMaterialIndex(Collider surfaceCollider, int triangleIndex, int materialCount)
        {
            if (materialCount <= 1 || triangleIndex < 0)
                return 0;

            if (surfaceCollider is not MeshCollider meshCollider || meshCollider.sharedMesh == null)
                return 0;

            int[] subMeshByTriangle = GetSubMeshLookup(meshCollider.sharedMesh);
            if (triangleIndex >= subMeshByTriangle.Length)
                return 0;

            return subMeshByTriangle[triangleIndex];
        }

        private static int[] GetSubMeshLookup(Mesh mesh)
        {
            if (SubMeshLookupCache.TryGetValue(mesh, out int[] cachedLookup))
                return cachedLookup;

            int triangleCount = mesh.triangles.Length / 3;
            int[] lookup = new int[triangleCount];
            int triangleCursor = 0;

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                int subMeshTriangleCount = mesh.GetTriangles(subMeshIndex).Length / 3;

                for (int localTriangleIndex = 0; localTriangleIndex < subMeshTriangleCount && triangleCursor < lookup.Length; localTriangleIndex++)
                {
                    lookup[triangleCursor] = subMeshIndex;
                    triangleCursor++;
                }
            }

            SubMeshLookupCache.Add(mesh, lookup);
            return lookup;
        }

        private static bool TryGetMaterialColor(Material material, out Color color)
        {
            color = Color.white;

            if (material == null)
                return false;

            if (material.HasProperty(BaseColorId))
            {
                color = material.GetColor(BaseColorId);
                return true;
            }

            if (material.HasProperty(ColorId))
            {
                color = material.GetColor(ColorId);
                return true;
            }

            return false;
        }
    }
}