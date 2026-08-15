using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests
{
    public sealed class HeroCarLodPolicyTests
    {
        [Test]
        public void HeroLodContractIsInternallyValid()
        {
            Assert.DoesNotThrow(HeroCarLodPolicy.ValidateContract);
            Assert.DoesNotThrow(HeroCarProductionQualityPolicy.ValidateContract);
        }

        [Test]
        public void LodTransitionsMatchReviewedScreenFractions()
        {
            Assert.That(HeroCarLodPolicy.TransitionFor(0), Is.EqualTo(.18f).Within(.0001f));
            Assert.That(HeroCarLodPolicy.TransitionFor(1), Is.EqualTo(.07f).Within(.0001f));
            Assert.That(HeroCarLodPolicy.TransitionFor(2), Is.EqualTo(.01f).Within(.0001f));
        }

        [TestCase(0, 2200, 5000)]
        [TestCase(1, 1100, 2400)]
        [TestCase(2, 650, 1400)]
        public void ProductionTargetsSitInsideMobileBudget(int lod, int vertices, int triangles)
        {
            Assert.That(HeroCarLodPolicy.IsWithinBudget(lod, vertices, triangles), Is.True);
        }

        [Test]
        public void RejectedBlockoutHeroCanNeverSatisfyProductionContract()
        {
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, 274, 476), Is.False,
                "The historically rejected 274v/476t Hero must remain below the production-art floor.");
            Assert.That(HeroCarLodPolicy.IsWithinBudget(1, 194, 332), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(2, 104, 180), Is.False);
        }

        [Test]
        public void GeneratedV2GeometryStillCannotPassProductionWithoutAuthoredSurfaceData()
        {
            Assert.That(
                HeroCarProductionQualityPolicy.MeetsProductionFloor(
                    lod: 0,
                    triangleCount: 4592,
                    hasUv0: false,
                    hasAuthoredNormals: false,
                    hasTextureMappedMaterial: false),
                Is.False,
                "More triangles alone must never turn the generated V2 preview into UART-003 production art.");
        }

        [Test]
        public void ProductionQualityRequiresUvNormalsAndTextureMapping()
        {
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 5000, false, true, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 5000, true, false, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 5000, true, true, false), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 5000, true, true, true), Is.True);
        }

        [Test]
        public void UnderDetailedOrOverBudgetGeometryIsRejected()
        {
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, HeroCarLodPolicy.MinimumVertices[0] - 1, HeroCarLodPolicy.MinimumTriangles[0]), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, HeroCarLodPolicy.MinimumVertices[0], HeroCarLodPolicy.MinimumTriangles[0] - 1), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, HeroCarLodPolicy.VertexBudgets[0] + 1, HeroCarLodPolicy.MinimumTriangles[0]), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, HeroCarLodPolicy.MinimumVertices[0], HeroCarLodPolicy.TriangleBudgets[0] + 1), Is.False);
        }

        [Test]
        public void ProductionTriangleFloorsDecreaseAtEveryLod()
        {
            Assert.That(HeroCarLodPolicy.MinimumTriangles[0], Is.GreaterThan(HeroCarLodPolicy.MinimumTriangles[1]));
            Assert.That(HeroCarLodPolicy.MinimumTriangles[1], Is.GreaterThan(HeroCarLodPolicy.MinimumTriangles[2]));
            Assert.That(HeroCarLodPolicy.ExpectedTriangles[0], Is.GreaterThan(HeroCarLodPolicy.ExpectedTriangles[1]));
            Assert.That(HeroCarLodPolicy.ExpectedTriangles[1], Is.GreaterThan(HeroCarLodPolicy.ExpectedTriangles[2]));
        }

        [Test]
        public void ProductionAndGeneratedPreviewResourcePathsAreSeparated()
        {
            Assert.That(
                HeroCarLodPolicy.ProductionResourcePath,
                Is.EqualTo("Art/Vehicles/HeroCar/Production/PF_Vehicle_AfareetKing_Production"));
            Assert.That(HeroCarLodPolicy.ResourcePath, Is.EqualTo(HeroCarLodPolicy.ProductionResourcePath));
            Assert.That(
                HeroCarLodPolicy.GeneratedPreviewResourcePath,
                Is.EqualTo("Art/Vehicles/HeroCar/Generated/PF_Vehicle_AfareetKing_PreviewV2"));
            Assert.That(HeroCarLodPolicy.GeneratedPreviewResourcePath, Is.Not.EqualTo(HeroCarLodPolicy.ProductionResourcePath));
        }
    }
}
