using System;
using System.Collections.Generic;

namespace Afareet.GarageRuntime
{
    public sealed class GarageVehicleDetail
    {
        public GarageVehicleDefinition Definition { get; }
        public GarageNormalizedStats NormalizedStats { get; }
        public GarageCosmeticSelection Selection { get; }
        public bool IsUnlocked { get; }
        public bool IsEquipped { get; }

        public GarageVehicleDetail(
            GarageVehicleDefinition definition,
            GarageNormalizedStats normalizedStats,
            GarageCosmeticSelection selection,
            bool isUnlocked,
            bool isEquipped)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            NormalizedStats = normalizedStats ?? throw new ArgumentNullException(nameof(normalizedStats));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            IsUnlocked = isUnlocked;
            IsEquipped = isEquipped;
        }
    }

    public sealed class GarageService
    {
        private readonly GarageCatalog catalog;
        private HashSet<string> unlocked;
        private GarageState state;

        public GarageCatalog Catalog => catalog;
        public GarageState State => state;
        public event Action<GarageState> StateChanged;

        public GarageService(
            GarageCatalog catalog,
            IEnumerable<string> unlockedVehicleIds = null,
            GarageState initialState = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            unlocked = BuildUnlockLookup(unlockedVehicleIds);
            state = ValidateAndNormalizeState(initialState ?? GarageState.Empty());
        }

        public IReadOnlyList<GarageVehicleAvailability> ListVehicles(bool unlockedOnly = false)
        {
            var all = catalog.GetAvailability(unlocked);
            if (!unlockedOnly) return all;

            var result = new List<GarageVehicleAvailability>();
            for (var index = 0; index < all.Count; index++)
                if (all[index].IsUnlocked) result.Add(all[index]);
            return result.AsReadOnly();
        }

        public GarageVehicleDetail GetDetail(string vehicleId)
        {
            var definition = catalog.GetRequired(vehicleId);
            var selection = state.GetSelectionOrDefault(definition);
            definition.Cosmetics.ValidateOrThrow(selection, definition.Id);
            return new GarageVehicleDetail(
                definition,
                catalog.NormalizeStats(vehicleId),
                selection,
                unlocked.Contains(vehicleId),
                StringComparer.Ordinal.Equals(state.EquippedVehicleId, vehicleId));
        }

        public GarageState Equip(string vehicleId)
        {
            RequireUnlocked(vehicleId);
            var next = state.WithEquipped(vehicleId);
            Commit(next);
            return state;
        }

        public GarageState Customize(string vehicleId, GarageCosmeticSelection selection)
        {
            RequireUnlocked(vehicleId);
            var definition = catalog.GetRequired(vehicleId);
            definition.Cosmetics.ValidateOrThrow(selection, vehicleId);
            var next = state.WithSelection(vehicleId, selection);
            Commit(next);
            return state;
        }

        public GarageState ResetCustomization(string vehicleId)
        {
            RequireUnlocked(vehicleId);
            var definition = catalog.GetRequired(vehicleId);
            var next = state.WithSelection(vehicleId, definition.Cosmetics.CreateDefaultSelection());
            Commit(next);
            return state;
        }

        public GarageState ReplaceUnlockedVehicleIds(IEnumerable<string> unlockedVehicleIds)
        {
            unlocked = BuildUnlockLookup(unlockedVehicleIds);
            var normalized = ValidateAndNormalizeState(state);
            Commit(normalized);
            return state;
        }

        public bool IsUnlocked(string vehicleId)
        {
            catalog.GetRequired(vehicleId);
            return unlocked.Contains(vehicleId);
        }

        private void Commit(GarageState next)
        {
            state = next ?? throw new ArgumentNullException(nameof(next));
            StateChanged?.Invoke(state);
        }

        private GarageState ValidateAndNormalizeState(GarageState candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));

            foreach (var pair in candidate.Selections)
            {
                var definition = catalog.GetRequired(pair.Key);
                definition.Cosmetics.ValidateOrThrow(pair.Value, pair.Key);
            }

            var equipped = candidate.EquippedVehicleId;
            if (equipped == null || !catalog.TryGet(equipped, out _) || !unlocked.Contains(equipped))
                equipped = FirstUnlockedVehicleId();

            return new GarageState(equipped, candidate.Selections);
        }

        private HashSet<string> BuildUnlockLookup(IEnumerable<string> unlockedVehicleIds)
        {
            var result = new HashSet<string>(StringComparer.Ordinal)
            {
                GarageCatalog.StarterVehicleId
            };

            if (unlockedVehicleIds == null) return result;
            foreach (var id in unlockedVehicleIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Garage unlock ids must be non-blank.", nameof(unlockedVehicleIds));
                catalog.GetRequired(id);
                result.Add(id);
            }
            return result;
        }

        private string FirstUnlockedVehicleId()
        {
            for (var index = 0; index < catalog.Vehicles.Count; index++)
            {
                var id = catalog.Vehicles[index].Id;
                if (unlocked.Contains(id)) return id;
            }
            throw new InvalidOperationException("Garage must always have at least one unlocked vehicle.");
        }

        private void RequireUnlocked(string vehicleId)
        {
            catalog.GetRequired(vehicleId);
            if (!unlocked.Contains(vehicleId))
                throw new InvalidOperationException($"Garage vehicle '{vehicleId}' is locked.");
        }
    }
}
