using System;

namespace Afareet.Vehicle
{
    public static class HeroCarLodPolicy
    {
        public const string ResourcePath = "Art/Vehicles/HeroCar/Generated/PF_Vehicle_AfareetKing_Production";

        public const float Lod0Transition = 0.18f;
        public const float Lod1Transition = 0.07f;
        public const float Lod2Transition = 0.01f;

        // Compatibility targets used by diagnostics. Acceptance is range based because
        // authored hard-surface details may legitimately move the exact vertex count.
        public static readonly int[] ExpectedVertices = { 2200, 1100, 650 };
        public static readonly int[] ExpectedTriangles = { 5000, 2400, 1400 };

        public static readonly int[] MinimumVertices = { 1500, 800, 500 };
        public static readonly int[] VertexBudgets = { 5000, 2800, 1800 };
        public static readonly int[] MinimumTriangles = { 3500, 1600, 900 };
        public static readonly int[] TriangleBudgets = { 7500, 4000, 2500 };

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
            if (lod < 0 || lod >= TriangleBudgets.Length) return false;
            return vertexCount >= MinimumVertices[lod] &&
                   vertexCount <= VertexBudgets[lod] &&
                   triangleCount >= MinimumTriangles[lod] &&
                   triangleCount <= TriangleBudgets[lod];
        }

        public static void ValidateContract()
        {
            if (!(Lod0Transition > Lod1Transition && Lod1Transition > Lod2Transition && Lod2Transition > 0f))
                throw new InvalidOperationException("Hero LOD screen-height thresholds must be strictly descending and positive.");

            if (ExpectedVertices.Length != 3 || ExpectedTriangles.Length != 3 ||
                MinimumVertices.Length != 3 || VertexBudgets.Length != 3 ||
                MinimumTriangles.Length != 3 || TriangleBudgets.Length != 3)
                throw new InvalidOperationException("Hero production geometry policy must define exactly three LOD levels.");

            for (var i = 0; i < 3; i++)
            {
                if (MinimumVertices[i] <= 0 || MinimumTriangles[i] <= 0)
                    throw new InvalidOperationException($"Hero LOD{i} production floors must be positive.");
                if (MinimumVertices[i] >= VertexBudgets[i] || MinimumTriangles[i] >= TriangleBudgets[i])
                    throw new InvalidOperationException($"Hero LOD{i} production floor must remain below its mobile budget.");
                if (!IsWithinBudget(i, ExpectedVertices[i], ExpectedTriangles[i]))
                    throw new InvalidOperationException($"Hero LOD{i} diagnostic target must sit inside its production range.");
            }

            if (!(ExpectedTriangles[0] > ExpectedTriangles[1] && ExpectedTriangles[1] > ExpectedTriangles[2]))
                throw new InvalidOperationException("Hero triangle targets must decrease with distance.");
            if (!(MinimumTriangles[0] > MinimumTriangles[1] && MinimumTriangles[1] > MinimumTriangles[2]))
                throw new InvalidOperationException("Hero minimum triangle floors must decrease with distance.");
        }
    }
}
