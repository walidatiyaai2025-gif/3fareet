using System;
using System.Collections.Generic;
using Afareet.GarageRuntime;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class GarageCatalogTests
    {
        [Test]
        public void DefaultCatalogDefinesFourStableVehicleArchetypes()
        {
            var catalog = GarageCatalog.CreateDefault();

            Assert.That(catalog.SchemaVersion, Is.EqualTo(GarageCatalog.CurrentSchemaVersion));
            Assert.That(catalog.Vehicles.Count, Is.EqualTo(4));
            Assert.That(catalog.Vehicles[0].Id, Is.EqualTo("afareet_king"));
            Assert.That(catalog.Vehicles[1].Id, Is.EqualTo("wedge_coupe"));
            Assert.That(catalog.Vehicles[2].Id, Is.EqualTo("fastback_muscle"));
            Assert.That(catalog.Vehicles[3].Id, Is.EqualTo("djinn_spirit"));
            Assert.That(catalog.Vehicles[0].Archetype, Is.EqualTo(GarageVehicleArchetype.Hero));
            Assert.That(catalog.Vehicles[3].Archetype, Is.EqualTo(GarageVehicleArchetype.CompactPrototype));
        }

        [Test]
        public void StarterIsAlwaysUnlockedAndFilteringPreservesCatalogOrder()
        {
            var catalog = GarageCatalog.CreateDefault();
            var unlocked = catalog.GetUnlocked(new[] { "djinn_spirit", "wedge_coupe" });

            Assert.That(unlocked.Count, Is.EqualTo(3));
            Assert.That(unlocked[0].Id, Is.EqualTo("afareet_king"));
            Assert.That(unlocked[1].Id, Is.EqualTo("wedge_coupe"));
            Assert.That(unlocked[2].Id, Is.EqualTo("djinn_spirit"));
        }

        [Test]
        public void UnknownUnlockIdIsRejectedInsteadOfSilentlyIgnored()
        {
            var catalog = GarageCatalog.CreateDefault();
            Assert.Throws<ArgumentException>(() => catalog.GetUnlocked(new[] { "unknown_vehicle" }));
        }

        [Test]
        public void DuplicateVehicleIdsAreRejected()
        {
            var catalog = GarageCatalog.CreateDefault();
            var first = catalog.Vehicles[0];
            Assert.Throws<ArgumentException>(() => new GarageCatalog(
                GarageCatalog.CurrentSchemaVersion,
                new[] { first, first }));
        }

        [Test]
        public void NormalizedStatsStayWithinZeroToOneAndPreserveRelativeStrengths()
        {
            var catalog = GarageCatalog.CreateDefault();
            foreach (var definition in catalog.Vehicles)
            {
                var normalized = catalog.NormalizeStats(definition.Id);
                Assert.That(normalized.TopSpeed, Is.InRange(0f, 1f));
                Assert.That(normalized.Acceleration, Is.InRange(0f, 1f));
                Assert.That(normalized.Handling, Is.InRange(0f, 1f));
                Assert.That(normalized.Drift, Is.InRange(0f, 1f));
                Assert.That(normalized.Spirit, Is.InRange(0f, 1f));
            }

            Assert.That(
                catalog.NormalizeStats("wedge_coupe").TopSpeed,
                Is.GreaterThan(catalog.NormalizeStats("djinn_spirit").TopSpeed));
            Assert.That(
                catalog.NormalizeStats("djinn_spirit").Handling,
                Is.GreaterThan(catalog.NormalizeStats("fastback_muscle").Handling));
        }

        [Test]
        public void CosmeticDefaultsAreMembersOfAllowedSets()
        {
            var catalog = GarageCatalog.CreateDefault();
            foreach (var definition in catalog.Vehicles)
            {
                var selection = definition.Cosmetics.CreateDefaultSelection();
                Assert.That(definition.Cosmetics.Allows(selection), Is.True, definition.Id);
            }
        }
    }
}
