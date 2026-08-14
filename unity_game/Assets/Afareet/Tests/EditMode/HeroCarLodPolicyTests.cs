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

        [TestCase(0, 274, 476)]
        [TestCase(1, 194, 332)]
        [TestCase(2, 104, 180)]
        public void AuthoredMeshesMatchExactBudgetContract(int lod, int vertices, int triangles)
        {
            Assert.That(HeroCarLodPolicy.IsWithinBudget(lod, vertices, triangles), Is.True);
        }

        [Test]
        public void WrongOrOverBudgetGeometryIsRejected()
        {
            Assert.That(HeroCarLodPolicy.IsWithinBudget(0, 274, 601), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(1, 195, 332), Is.False);
            Assert.That(HeroCarLodPolicy.IsWithinBudget(2, 104, 221), Is.False);
        }

        [Test]
        public void TriangleCountsDecreaseAtEveryLod()
        {
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
