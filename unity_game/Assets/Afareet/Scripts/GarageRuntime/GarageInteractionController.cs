using System;
using System.Collections.Generic;

namespace Afareet.GarageRuntime
{
    public enum GarageCosmeticChannel
    {
        Paint = 0,
        Wheels = 1,
        Trail = 2,
        Spirit = 3
    }

    public sealed class GarageInteractionSnapshot
    {
        public int SelectedIndex { get; }
        public int VehicleCount { get; }
        public GarageVehicleDetail Detail { get; }

        public GarageInteractionSnapshot(int selectedIndex, int vehicleCount, GarageVehicleDetail detail)
        {
            if (selectedIndex < 0) throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            if (vehicleCount <= 0) throw new ArgumentOutOfRangeException(nameof(vehicleCount));
            if (selectedIndex >= vehicleCount) throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            SelectedIndex = selectedIndex;
            VehicleCount = vehicleCount;
            Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        }
    }

    public sealed class GarageInteractionController
    {
        private readonly GarageService service;
        private string selectedVehicleId;

        public string SelectedVehicleId => selectedVehicleId;
        public GarageInteractionSnapshot Snapshot => BuildSnapshot();

        public event Action<GarageInteractionSnapshot> SelectionChanged;
        public event Action<GarageInteractionSnapshot> InteractionChanged;

        public GarageInteractionController(GarageService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            var vehicles = service.ListVehicles();
            if (vehicles.Count == 0)
                throw new InvalidOperationException("Garage interaction requires at least one vehicle.");

            selectedVehicleId = service.State.EquippedVehicleId;
            if (string.IsNullOrWhiteSpace(selectedVehicleId) || !service.Catalog.TryGet(selectedVehicleId, out _))
                selectedVehicleId = vehicles[0].Definition.Id;

            service.StateChanged += OnServiceStateChanged;
        }

        public GarageInteractionSnapshot Select(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage selected vehicle id is required.", nameof(vehicleId));
            service.Catalog.GetRequired(vehicleId);
            if (StringComparer.Ordinal.Equals(selectedVehicleId, vehicleId))
                return BuildSnapshot();

            selectedVehicleId = vehicleId;
            var snapshot = BuildSnapshot();
            SelectionChanged?.Invoke(snapshot);
            InteractionChanged?.Invoke(snapshot);
            return snapshot;
        }

        public GarageInteractionSnapshot MoveSelection(int delta)
        {
            if (delta == 0) return BuildSnapshot();
            var vehicles = service.ListVehicles();
            var currentIndex = IndexOf(vehicles, selectedVehicleId);
            if (currentIndex < 0) currentIndex = 0;
            var nextIndex = Wrap(currentIndex + delta, vehicles.Count);
            return Select(vehicles[nextIndex].Definition.Id);
        }

        public GarageInteractionSnapshot EquipSelected()
        {
            service.Equip(selectedVehicleId);
            return BuildSnapshot();
        }

        public GarageInteractionSnapshot CycleCosmetic(GarageCosmeticChannel channel, int delta)
        {
            if (!Enum.IsDefined(typeof(GarageCosmeticChannel), channel))
                throw new ArgumentOutOfRangeException(nameof(channel));
            if (delta == 0) return BuildSnapshot();

            var detail = service.GetDetail(selectedVehicleId);
            if (!detail.IsUnlocked)
                throw new InvalidOperationException($"Garage vehicle '{selectedVehicleId}' is locked.");

            var cosmetics = detail.Definition.Cosmetics;
            var current = detail.Selection;
            var next = channel switch
            {
                GarageCosmeticChannel.Paint => new GarageCosmeticSelection(
                    Cycle(cosmetics.PaintIds, current.PaintId, delta),
                    current.WheelId,
                    current.TrailId,
                    current.SpiritId),
                GarageCosmeticChannel.Wheels => new GarageCosmeticSelection(
                    current.PaintId,
                    Cycle(cosmetics.WheelIds, current.WheelId, delta),
                    current.TrailId,
                    current.SpiritId),
                GarageCosmeticChannel.Trail => new GarageCosmeticSelection(
                    current.PaintId,
                    current.WheelId,
                    Cycle(cosmetics.TrailIds, current.TrailId, delta),
                    current.SpiritId),
                GarageCosmeticChannel.Spirit => new GarageCosmeticSelection(
                    current.PaintId,
                    current.WheelId,
                    current.TrailId,
                    Cycle(cosmetics.SpiritIds, current.SpiritId, delta)),
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };

            service.Customize(selectedVehicleId, next);
            return BuildSnapshot();
        }

        public GarageInteractionSnapshot ResetSelectedCustomization()
        {
            service.ResetCustomization(selectedVehicleId);
            return BuildSnapshot();
        }

        private GarageInteractionSnapshot BuildSnapshot()
        {
            var vehicles = service.ListVehicles();
            var selectedIndex = IndexOf(vehicles, selectedVehicleId);
            if (selectedIndex < 0)
                throw new InvalidOperationException($"Selected Garage vehicle '{selectedVehicleId}' is not in the catalog.");
            return new GarageInteractionSnapshot(
                selectedIndex,
                vehicles.Count,
                service.GetDetail(selectedVehicleId));
        }

        private void OnServiceStateChanged(GarageState _)
        {
            InteractionChanged?.Invoke(BuildSnapshot());
        }

        private static int IndexOf(IReadOnlyList<GarageVehicleAvailability> vehicles, string vehicleId)
        {
            for (var index = 0; index < vehicles.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(vehicles[index].Definition.Id, vehicleId))
                    return index;
            }
            return -1;
        }

        private static string Cycle(IReadOnlyList<string> options, string current, int delta)
        {
            if (options == null || options.Count == 0)
                throw new InvalidOperationException("Garage cosmetic option list is empty.");
            var currentIndex = -1;
            for (var index = 0; index < options.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(options[index], current))
                {
                    currentIndex = index;
                    break;
                }
            }
            if (currentIndex < 0)
                throw new InvalidOperationException($"Current Garage cosmetic '{current}' is not present in its option set.");
            return options[Wrap(currentIndex + delta, options.Count)];
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var result = value % count;
            return result < 0 ? result + count : result;
        }
    }
}
