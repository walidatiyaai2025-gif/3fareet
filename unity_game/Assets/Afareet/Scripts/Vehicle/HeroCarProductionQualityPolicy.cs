using System;

namespace Afareet.Vehicle
{
    /// <summary>
    /// UART-003 production-art floor. Geometry density alone is insufficient: an accepted
    /// Hero must also carry UV0, authored normals and at least one texture-mapped material.
    /// Generated/editor-preview meshes deliberately fail this contract.
    /// </summary>
    public static class HeroCarProductionQualityPolicy
    {
        public static readonly int[] MinimumTriangles = { 2500, 1200, 500 };
        public static readonly int[] MaximumTriangles = { 20000, 10000, 4000 };

        public const bool RequireUv0 = true;
        public const bool RequireAuthoredNormals = true;
        public const bool RequireTextureMappedMaterial = true;

        public static bool MeetsProductionFloor(
            int lod,
            int triangleCount,
            bool hasUv0,
            bool hasAuthoredNormals,
            bool hasTextureMappedMaterial)
        {
            if (lod < 0 || lod >= MinimumTriangles.Length) return false;
            if (triangleCount < MinimumTriangles[lod] || triangleCount > MaximumTriangles[lod]) return false;
            if (RequireUv0 && !hasUv0) return false;
            if (RequireAuthoredNormals && !hasAuthoredNormals) return false;
            if (RequireTextureMappedMaterial && !hasTextureMappedMaterial) return false;
            return true;
        }

        public static void ValidateContract()
        {
            if (MinimumTriangles.Length != 3 || MaximumTriangles.Length != 3)
                throw new InvalidOperationException("UART-003 production Hero must define exactly three LOD quality bands.");

            for (var lod = 0; lod < 3; lod++)
            {
                if (MinimumTriangles[lod] <= 0 || MaximumTriangles[lod] <= MinimumTriangles[lod])
                    throw new InvalidOperationException($"UART-003 invalid production triangle band for LOD{lod}.");
            }

            if (!(MinimumTriangles[0] > MinimumTriangles[1] && MinimumTriangles[1] > MinimumTriangles[2]))
                throw new InvalidOperationException("UART-003 production triangle floors must decrease across LODs.");
        }
    }
}
