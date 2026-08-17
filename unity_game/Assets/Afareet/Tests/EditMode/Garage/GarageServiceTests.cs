using System;
using Afareet.GarageRuntime;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class GarageServiceTests
    {
        [Test]
        public void EmptyStateAutoEquipsStarter()
        {
            var service = new GarageService(GarageCatalog.CreateDefault());
            Assert.That(service.State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            Assert.That(service.IsUnlocked(GarageCatalog.StarterVehicleId), Is.True);
        }

        [Test]
        public void LockedVehicleCannotBeEquippedOrCustomized()
        {
            var service = new GarageService(GarageCatalog.CreateDefault());
            Assert.Throws<InvalidOperationException>(() => service.Equip("wedge_coupe"));
            Assert.Throws<InvalidOperationException>(() => service.Customize(
                "wedge_coupe",
                service.Catalog.GetRequired("wedge_coupe").Cosmetics.CreateDefaultSelection()));
        }

        [Test]
        public void CareerUnlockCanBeEquippedAndCustomized()
        {
            var service = new GarageService(
                GarageCatalog.CreateDefault(),
                new[] { "wedge_coupe" });

            service.Equip("wedge_coupe");
            var definition = service.Catalog.GetRequired("wedge_coupe");
            var custom = new GarageCosmeticSelection(
                "obsidian",
                "shadow-rim",
                "purple-wisp",
                "afareet");
            service.Customize("wedge_coupe", custom);

            var detail = service.GetDetail("wedge_coupe");
            Assert.That(detail.IsUnlocked, Is.True);
            Assert.That(detail.IsEquipped, Is.True);
            Assert.That(detail.Selection, Is.EqualTo(custom));
        }

        [Test]
        public void CosmeticOutsideVehicleSetIsRejected()
        {
            var service = new GarageService(
                GarageCatalog.CreateDefault(),
                new[] { "fastback_muscle" });

            Assert.Throws<InvalidOperationException>(() => service.Customize(
                "fastback_muscle",
                new GarageCosmeticSelection("not-a-paint", "shadow-rim", "purple-wisp", "afareet")));
        }

        [Test]
        public void RemovingUnlockFallsBackToStarterWithoutDeletingSavedCosmetics()
        {
            var catalog = GarageCatalog.CreateDefault();
            var service = new GarageService(catalog, new[] { "wedge_coupe" });
            var custom = new GarageCosmeticSelection("obsidian", "shadow-rim", "purple-wisp", "afareet");
            service.Customize("wedge_coupe", custom);
            service.Equip("wedge_coupe");

            service.ReplaceUnlockedVehicleIds(Array.Empty<string>());

            Assert.That(service.State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            Assert.That(service.State.TryGetSelection("wedge_coupe", out var saved), Is.True);
            Assert.That(saved, Is.EqualTo(custom));
        }

        [Test]
        public void StateChangedFiresForMutations()
        {
            var service = new GarageService(GarageCatalog.CreateDefault(), new[] { "djinn_spirit" });
            var notifications = 0;
            service.StateChanged += _ => notifications++;

            service.Equip("djinn_spirit");
            service.ResetCustomization("djinn_spirit");

            Assert.That(notifications, Is.EqualTo(2));
        }
    }
}
