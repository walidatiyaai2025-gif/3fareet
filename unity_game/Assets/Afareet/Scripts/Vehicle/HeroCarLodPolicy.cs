using System;

namespace Afareet.Vehicle
{
    public static class HeroCarLodPolicy
    {
        public const string ProductionResourcePath = "Art/Vehicles/HeroCar/Production/PF_Vehicle_AfareetKing_Production";
        public const string DevelopmentFallbackResourcePath = "Art/Vehicles/HeroCar/Generated/PF_Vehicle_AfareetKing_Production";

        // Compatibility alias for callers that need the accepted production path.
        public const string ResourcePath = ProductionResourcePath;

        public const float Lod0Transition = 0.18f;
        public const float Lod1Transition = 0.07f;
        public const float Lod2Transition = 0.01f;

        // Legacy generated fallback contract. These counts are intentionally not the
        // UART-003 production-art acceptance floor; see HeroCarProductionQualityPolicy.
        public static readonly int[] ExpectedVertices = { 274, 194, 104 };
        public static readonly int[] ExpectedTriangles = { 476, 332, 180 };
        public static readonly int[] TriangleBudgets = { 600, 400, 220 };

        public static float TransitionFor(int lod)
        {
            return lod switch
            {
                0 => Lod0Transition,
                1 => Lod1Transition,
                2 => Lod2Transition,
                _ => throw new ArgumentOutOfRangeException(nameof(lod))
            };
        }

        public static bool IsWithinBudget(int lod, int vertexCount, int triangleCount)
        {
            if (lod < 0 || lod >= ExpectedTriangles.Length) return false;
            if (vertexCount <= 0 || triangleCount <= 0) return false;
            return vertexCount == ExpectedVertices[lod] &&
                   triangleCount == ExpectedTriangles[lod] &&
                   triangleCount <= TriangleBudgets[lod];
        }

        public static void ValidateContract()
        {
            if (string.Equals(ProductionResourcePath, DevelopmentFallbackResourcePath, StringComparison.Ordinal))
                throw new InvalidOperationException("UART-003 production and development-fallback resource paths must stay distinct.");

            if (!(Lod0Transition > Lod1Transition && Lod1Transition > Lod2Transition && Lod2Transition > 0f))
                throw new InvalidOperationException("Hero LOD screen-height thresholds must be strictly descending and positive.");

            for (var i = 0; i < ExpectedTriangles.Length; i++)
            {
                if (!IsWithinBudget(i, ExpectedVertices[i], ExpectedTriangles[i]))
                    throw new InvalidOperationException($"Hero LOD{i} development-fallback mesh budget contract is invalid.");
            }

            if (!(ExpectedTriangles[0] > ExpectedTriangles[1] && ExpectedTriangles[1] > ExpectedTriangles[2]))
                throw new InvalidOperationException("Hero triangle counts must decrease with distance.");
        }
    }
}
