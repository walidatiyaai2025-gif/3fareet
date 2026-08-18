using System;
using System.Collections.Generic;

namespace Afareet.GarageRuntime
{
    public sealed class GarageCosmeticSelection
    {
        public string PaintId { get; }
        public string WheelId { get; }
        public string TrailId { get; }
        public string SpiritId { get; }

        public GarageCosmeticSelection(string paintId, string wheelId, string trailId, string spiritId)
        {
            PaintId = RequireId(paintId, nameof(paintId));
            WheelId = RequireId(wheelId, nameof(wheelId));
            TrailId = RequireId(trailId, nameof(trailId));
            SpiritId = RequireId(spiritId, nameof(spiritId));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GarageCosmeticSelection other)) return false;
            return StringComparer.Ordinal.Equals(PaintId, other.PaintId) &&
                   StringComparer.Ordinal.Equals(WheelId, other.WheelId) &&
                   StringComparer.Ordinal.Equals(TrailId, other.TrailId) &&
                   StringComparer.Ordinal.Equals(SpiritId, other.SpiritId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(PaintId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(WheelId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TrailId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SpiritId);
                return hash;
            }
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Garage cosmetic ids must be non-blank.", parameterName);
            return value;
        }
    }

    public sealed class GarageState
    {
        private readonly Dictionary<string, GarageCosmeticSelection> selections;

        public string EquippedVehicleId { get; }
        public IReadOnlyDictionary<string, GarageCosmeticSelection> Selections => selections;

        public GarageState(
            string equippedVehicleId,
            IEnumerable<KeyValuePair<string, GarageCosmeticSelection>> selections = null)
        {
            if (equippedVehicleId != null && string.IsNullOrWhiteSpace(equippedVehicleId))
                throw new ArgumentException("Equipped Garage vehicle id must be null or non-blank.", nameof(equippedVehicleId));

            EquippedVehicleId = equippedVehicleId;
            this.selections = CopySelections(selections, nameof(selections));
        }

        public static GarageState Empty() => new GarageState(null);

        public bool TryGetSelection(string vehicleId, out GarageCosmeticSelection selection)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                selection = null;
                return false;
            }
            return selections.TryGetValue(vehicleId, out selection);
        }

        public GarageCosmeticSelection GetSelectionOrDefault(GarageVehicleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return selections.TryGetValue(definition.Id, out var selection)
                ? selection
                : definition.Cosmetics.CreateDefaultSelection();
        }

        public GarageState WithEquipped(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage equipped vehicle id is required.", nameof(vehicleId));
            return new GarageState(vehicleId, selections);
        }

        public GarageState WithSelection(string vehicleId, GarageCosmeticSelection selection)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage selection vehicle id is required.", nameof(vehicleId));
            if (selection == null) throw new ArgumentNullException(nameof(selection));

            var next = CopySelections(selections, nameof(selections));
            next[vehicleId] = selection;
            return new GarageState(EquippedVehicleId, next);
        }

        public GarageState WithoutSelection(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage selection vehicle id is required.", nameof(vehicleId));

            var next = CopySelections(selections, nameof(selections));
            next.Remove(vehicleId);
            return new GarageState(EquippedVehicleId, next);
        }

        private static Dictionary<string, GarageCosmeticSelection> CopySelections(
            IEnumerable<KeyValuePair<string, GarageCosmeticSelection>> source,
            string parameterName)
        {
            var copy = new Dictionary<string, GarageCosmeticSelection>(StringComparer.Ordinal);
            if (source == null) return copy;

            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Garage selection vehicle ids must be non-blank.", parameterName);
                if (pair.Value == null)
                    throw new ArgumentException($"Garage selection for '{pair.Key}' is null.", parameterName);
                if (!copy.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException($"Garage selection for '{pair.Key}' is duplicated.", parameterName);
            }

            return copy;
        }
    }
}
