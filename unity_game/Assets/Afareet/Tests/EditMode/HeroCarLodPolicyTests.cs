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
        public void ProductionPrefabResourcePathIsStable()
        {
            Assert.That(
                HeroCarLodPolicy.ResourcePath,
                Is.EqualTo("Art/Vehicles/HeroCar/Generated/PF_Vehicle_AfareetKing_Production"));
        }
    }
}
