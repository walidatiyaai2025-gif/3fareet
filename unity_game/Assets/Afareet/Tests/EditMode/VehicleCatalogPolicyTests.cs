using System;
using System.Linq;
using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests
{
    public sealed class VehicleCatalogPolicyTests
    {
        [Test]
        public void ValidateOrThrow_AcceptsStableUniqueCatalog()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[]
                {
                    Definition("afareet.king", 0.92f, VehicleUnlockRequirement.Always()),
                    Definition("cairo.wedge-01", 0.75f, VehicleUnlockRequirement.PlayerLevel(3))
                });

            Assert.DoesNotThrow(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [Test]
        public void ValidateOrThrow_RejectsUnsupportedSchema()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion + 1,
                new[] { Definition("afareet.king", 0.9f, VehicleUnlockRequirement.Always()) });

            Assert.Throws<ArgumentException>(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [Test]
        public void ValidateOrThrow_RejectsDuplicateIds()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[]
                {
                    Definition("afareet.king", 0.9f, VehicleUnlockRequirement.Always()),
                    Definition("afareet.king", 0.8f, VehicleUnlockRequirement.PlayerLevel(2))
                });

            Assert.Throws<ArgumentException>(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [TestCase("")]
        [TestCase("Afareet.King")]
        [TestCase("afareet king")]
        [TestCase("/afareet.king")]
        [TestCase("afareet/king")]
        public void IsTransportSafeId_RejectsNonCanonicalIds(string id)
        {
            Assert.That(VehicleCatalogPolicy.IsTransportSafeId(id), Is.False);
        }

        [TestCase("afareet.king")]
        [TestCase("cairo.wedge-01")]
        [TestCase("rival_03")]
        public void IsTransportSafeId_AcceptsOpaqueCanonicalIds(string id)
        {
            Assert.That(VehicleCatalogPolicy.IsTransportSafeId(id), Is.True);
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        public void ValidateOrThrow_RejectsOutOfRangeNormalizedStats(float topSpeed)
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[] { Definition("afareet.king", topSpeed, VehicleUnlockRequirement.Always()) });

            Assert.Throws<ArgumentException>(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [Test]
        public void ValidateOrThrow_RejectsNonFiniteNormalizedStats()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[] { Definition("afareet.king", float.NaN, VehicleUnlockRequirement.Always()) });

            Assert.Throws<ArgumentException>(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [Test]
        public void ValidateOrThrow_RejectsInvalidAlwaysThreshold()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[]
                {
                    Definition(
                        "afareet.king",
                        0.9f,
                        new VehicleUnlockRequirement(VehicleUnlockKind.Always, 1))
                });

            Assert.Throws<ArgumentException>(() => VehicleCatalogPolicy.ValidateOrThrow(catalog));
        }

        [Test]
        public void FilterUnlocked_PreservesCatalogOrderAndAppliesRequirements()
        {
            var catalog = new VehicleCatalog(
                VehicleCatalog.CurrentSchemaVersion,
                new[]
                {
                    Definition("starter.king", 0.72f, VehicleUnlockRequirement.Always()),
                    Definition("career.scarab", 0.76f, VehicleUnlockRequirement.CareerStars(8)),
                    Definition("level.sphinx", 0.80f, VehicleUnlockRequirement.PlayerLevel(5)),
                    Definition("career.pharaoh", 0.88f, VehicleUnlockRequirement.CareerStars(20))
                });

            var unlocked = VehicleCatalogPolicy.FilterUnlocked(
                catalog,
                new VehicleProgressSnapshot(playerLevel: 5, careerStars: 8));

            CollectionAssert.AreEqual(
                new[] { "starter.king", "career.scarab", "level.sphinx" },
                unlocked.Select(definition => definition.Id).ToArray());
        }

        [Test]
        public void IsUnlocked_UsesInclusiveThresholds()
        {
            var progress = new VehicleProgressSnapshot(playerLevel: 4, careerStars: 10);

            Assert.That(
                VehicleCatalogPolicy.IsUnlocked(VehicleUnlockRequirement.PlayerLevel(4), progress),
                Is.True);
            Assert.That(
                VehicleCatalogPolicy.IsUnlocked(VehicleUnlockRequirement.CareerStars(10), progress),
                Is.True);
            Assert.That(
                VehicleCatalogPolicy.IsUnlocked(VehicleUnlockRequirement.PlayerLevel(5), progress),
                Is.False);
        }

        private static VehicleDefinition Definition(
            string id,
            float topSpeed,
            VehicleUnlockRequirement unlockRequirement)
        {
            return new VehicleDefinition(
                id,
                "vehicle." + id + ".name",
                topSpeed,
                acceleration: 0.7f,
                handling: 0.8f,
                drift: 0.65f,
                unlockRequirement: unlockRequirement);
        }
    }
}
