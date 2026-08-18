using System;
using Afareet.GarageRuntime;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class GarageInteractionControllerTests
    {
        [Test]
        public void StartsOnEquippedVehicleAndBrowsesAllCardsWithWrapAround()
        {
            var service = new GarageService(
                GarageCatalog.CreateDefault(),
                new[] { "wedge_coupe" });
            service.Equip("wedge_coupe");
            var controller = new GarageInteractionController(service);

            Assert.That(controller.SelectedVehicleId, Is.EqualTo("wedge_coupe"));
            Assert.That(controller.Snapshot.VehicleCount, Is.EqualTo(4));

            controller.MoveSelection(3);
            Assert.That(controller.SelectedVehicleId, Is.EqualTo("afareet_king"));
            controller.MoveSelection(-1);
            Assert.That(controller.SelectedVehicleId, Is.EqualTo("djinn_spirit"));
        }

        [Test]
        public void LockedCardMayBeInspectedButCannotBeEquippedOrCustomized()
        {
            var controller = new GarageInteractionController(
                new GarageService(GarageCatalog.CreateDefault()));

            var selected = controller.Select("fastback_muscle");

            Assert.That(selected.Detail.IsUnlocked, Is.False);
            Assert.Throws<InvalidOperationException>(() => controller.EquipSelected());
            Assert.Throws<InvalidOperationException>(() =>
                controller.CycleCosmetic(GarageCosmeticChannel.Paint, 1));
        }

        [Test]
        public void EquipSelectedDelegatesToAuthoritativeGarageService()
        {
            var service = new GarageService(
                GarageCatalog.CreateDefault(),
                new[] { "djinn_spirit" });
            var controller = new GarageInteractionController(service);
            controller.Select("djinn_spirit");

            var snapshot = controller.EquipSelected();

            Assert.That(service.State.EquippedVehicleId, Is.EqualTo("djinn_spirit"));
            Assert.That(snapshot.Detail.IsEquipped, Is.True);
        }

        [Test]
        public void CosmeticCyclingWrapsAndPreservesOtherChannels()
        {
            var service = new GarageService(GarageCatalog.CreateDefault());
            var controller = new GarageInteractionController(service);
            var before = controller.Snapshot.Detail.Selection;

            var after = controller.CycleCosmetic(GarageCosmeticChannel.Paint, -1).Detail.Selection;

            Assert.That(after.PaintId, Is.Not.EqualTo(before.PaintId));
            Assert.That(after.WheelId, Is.EqualTo(before.WheelId));
            Assert.That(after.TrailId, Is.EqualTo(before.TrailId));
            Assert.That(after.SpiritId, Is.EqualTo(before.SpiritId));
            Assert.That(service.State.TryGetSelection(GarageCatalog.StarterVehicleId, out var saved), Is.True);
            Assert.That(saved, Is.EqualTo(after));
        }

        [Test]
        public void InteractionEventsSeparateSelectionFromStateMutation()
        {
            var service = new GarageService(
                GarageCatalog.CreateDefault(),
                new[] { "wedge_coupe" });
            var controller = new GarageInteractionController(service);
            var selectionEvents = 0;
            var interactionEvents = 0;
            controller.SelectionChanged += _ => selectionEvents++;
            controller.InteractionChanged += _ => interactionEvents++;

            controller.Select("wedge_coupe");
            controller.EquipSelected();

            Assert.That(selectionEvents, Is.EqualTo(1));
            Assert.That(interactionEvents, Is.EqualTo(2));
        }

        [Test]
        public void UnknownSelectionFailsClosed()
        {
            var controller = new GarageInteractionController(
                new GarageService(GarageCatalog.CreateDefault()));

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                controller.Select("unknown_vehicle"));
        }
    }
}
