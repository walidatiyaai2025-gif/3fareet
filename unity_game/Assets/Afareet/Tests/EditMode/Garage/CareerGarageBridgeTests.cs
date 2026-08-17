using System;
using Afareet.CareerRuntime;
using Afareet.GarageRuntime;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class CareerGarageBridgeTests
    {
        [Test]
        public void StarterIsAvailableEvenWhenCareerProfileHasNoUnlocks()
        {
            var catalog = GarageCatalog.CreateDefault();
            var ids = CareerGarageBridge.ResolveUnlockedVehicleIds(CareerPlayerProfile.Empty(), catalog);

            Assert.That(ids.Count, Is.EqualTo(1));
            Assert.That(ids[0], Is.EqualTo(GarageCatalog.StarterVehicleId));
        }

        [Test]
        public void CareerVehicleUnlockFlowsIntoGarageService()
        {
            var profile = new CareerPlayerProfile(
                CareerProgress.Empty(),
                coins: 0,
                spirit: 0,
                unlockedVehicleIds: new[] { "djinn_spirit" });

            var service = CareerGarageBridge.CreateGarageService(profile);

            Assert.That(service.IsUnlocked(GarageCatalog.StarterVehicleId), Is.True);
            Assert.That(service.IsUnlocked("djinn_spirit"), Is.True);
            service.Equip("djinn_spirit");
            Assert.That(service.State.EquippedVehicleId, Is.EqualTo("djinn_spirit"));
        }

        [Test]
        public void ChapterOneVehicleRewardsResolveAgainstDefaultGarageCatalog()
        {
            Assert.DoesNotThrow(() => CareerGarageBridge.ValidateCareerVehicleRewardsOrThrow(
                ChapterOneCareerEventContent.CreateDefinitions(),
                GarageCatalog.CreateDefault()));
        }

        [Test]
        public void UnknownCareerUnlockIsRejectedInsteadOfCreatingGhostGarageVehicle()
        {
            var catalog = GarageCatalog.CreateDefault();
            var profile = new CareerPlayerProfile(
                CareerProgress.Empty(),
                0,
                0,
                new[] { "ghost_vehicle" });

            Assert.Throws<InvalidOperationException>(() =>
                CareerGarageBridge.ResolveUnlockedVehicleIds(profile, catalog));
        }

        [Test]
        public void RewardValidationFailsWhenCatalogOmitsCareerVehicle()
        {
            var defaultCatalog = GarageCatalog.CreateDefault();
            var starterOnly = new GarageCatalog(
                GarageCatalog.CurrentSchemaVersion,
                new[] { defaultCatalog.GetRequired(GarageCatalog.StarterVehicleId) });

            Assert.Throws<InvalidOperationException>(() =>
                CareerGarageBridge.ValidateCareerVehicleRewardsOrThrow(
                    ChapterOneCareerEventContent.CreateDefinitions(),
                    starterOnly));
        }
    }
}
