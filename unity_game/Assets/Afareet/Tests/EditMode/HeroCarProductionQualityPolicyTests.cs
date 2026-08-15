using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests
{
    public sealed class HeroCarProductionQualityPolicyTests
    {
        [Test]
        public void ProductionQualityContractIsInternallyValid()
        {
            Assert.DoesNotThrow(HeroCarProductionQualityPolicy.ValidateContract);
        }

        [TestCase(0, 2500)]
        [TestCase(1, 1200)]
        [TestCase(2, 500)]
        public void ProductionFloorAcceptsAuthoredMeshAtMinimumQuality(int lod, int triangles)
        {
            Assert.That(
                HeroCarProductionQualityPolicy.MeetsProductionFloor(
                    lod,
                    triangles,
                    hasUv0: true,
                    hasAuthoredNormals: true,
                    hasTextureMappedMaterial: true),
                Is.True);
        }

        [Test]
        public void LegacyGeneratedHeroCannotPassProductionFloor()
        {
            Assert.That(
                HeroCarProductionQualityPolicy.MeetsProductionFloor(
                    lod: 0,
                    triangleCount: HeroCarLodPolicy.ExpectedTriangles[0],
                    hasUv0: false,
                    hasAuthoredNormals: false,
                    hasTextureMappedMaterial: false),
                Is.False,
                "The 476-triangle textureless Hero is a development fallback, not UART-003 production art.");
        }

        [Test]
        public void MissingUvNormalsOrTextureMapFailsClosed()
        {
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 3000, false, true, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 3000, true, false, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 3000, true, true, false), Is.False);
        }

        [Test]
        public void ExcessivelyDenseMobileMeshIsRejected()
        {
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(0, 20001, true, true, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(1, 10001, true, true, true), Is.False);
            Assert.That(HeroCarProductionQualityPolicy.MeetsProductionFloor(2, 4001, true, true, true), Is.False);
        }
    }
}
