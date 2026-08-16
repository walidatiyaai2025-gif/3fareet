using System;
using System.Collections.Generic;

namespace Afareet.Vehicle
{
    public enum VehicleUnlockKind
    {
        Always = 0,
        PlayerLevel = 1,
        CareerStars = 2
    }

    public sealed class VehicleUnlockRequirement
    {
        public VehicleUnlockKind Kind { get; }
        public int Threshold { get; }

        public VehicleUnlockRequirement(VehicleUnlockKind kind, int threshold)
        {
            Kind = kind;
            Threshold = threshold;
        }

        public static VehicleUnlockRequirement Always()
        {
            return new VehicleUnlockRequirement(VehicleUnlockKind.Always, 0);
        }

        public static VehicleUnlockRequirement PlayerLevel(int level)
        {
            return new VehicleUnlockRequirement(VehicleUnlockKind.PlayerLevel, level);
        }

        public static VehicleUnlockRequirement CareerStars(int stars)
        {
            return new VehicleUnlockRequirement(VehicleUnlockKind.CareerStars, stars);
        }
    }

    public sealed class VehicleDefinition
    {
        public string Id { get; }
        public string DisplayNameKey { get; }
        public float TopSpeed { get; }
        public float Acceleration { get; }
        public float Handling { get; }
        public float Drift { get; }
        public VehicleUnlockRequirement UnlockRequirement { get; }

        public VehicleDefinition(
            string id,
            string displayNameKey,
            float topSpeed,
            float acceleration,
            float handling,
            float drift,
            VehicleUnlockRequirement unlockRequirement)
        {
            Id = id;
            DisplayNameKey = displayNameKey;
            TopSpeed = topSpeed;
            Acceleration = acceleration;
            Handling = handling;
            Drift = drift;
            UnlockRequirement = unlockRequirement;
        }
    }

    public sealed class VehicleCatalog
    {
        public const int CurrentSchemaVersion = 1;

        private readonly IReadOnlyList<VehicleDefinition> definitions;

        public int SchemaVersion { get; }
        public IReadOnlyList<VehicleDefinition> Definitions => definitions;

        public VehicleCatalog(int schemaVersion, IEnumerable<VehicleDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            SchemaVersion = schemaVersion;
            this.definitions = new List<VehicleDefinition>(definitions).AsReadOnly();
        }
    }

    public sealed class VehicleProgressSnapshot
    {
        public int PlayerLevel { get; }
        public int CareerStars { get; }

        public VehicleProgressSnapshot(int playerLevel, int careerStars)
        {
            if (playerLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel));
            }

            if (careerStars < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(careerStars));
            }

            PlayerLevel = playerLevel;
            CareerStars = careerStars;
        }
    }

    public static class VehicleCatalogPolicy
    {
        private const int MaxTransportIdLength = 64;

        public static void ValidateOrThrow(VehicleCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (catalog.SchemaVersion != VehicleCatalog.CurrentSchemaVersion)
            {
                throw new ArgumentException(
                    $"Unsupported vehicle catalog schema version {catalog.SchemaVersion}; expected {VehicleCatalog.CurrentSchemaVersion}.",
                    nameof(catalog));
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < catalog.Definitions.Count; index++)
            {
                var definition = catalog.Definitions[index];
                if (definition == null)
                {
                    throw new ArgumentException($"Vehicle definition at index {index} is null.", nameof(catalog));
                }

                if (!IsTransportSafeId(definition.Id))
                {
                    throw new ArgumentException(
                        $"Vehicle definition at index {index} has an invalid transport-safe id '{definition.Id ?? "<null>"}'.",
                        nameof(catalog));
                }

                if (!seenIds.Add(definition.Id))
                {
                    throw new ArgumentException($"Duplicate vehicle definition id '{definition.Id}'.", nameof(catalog));
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayNameKey))
                {
                    throw new ArgumentException(
                        $"Vehicle definition '{definition.Id}' must provide a display-name localization key.",
                        nameof(catalog));
                }

                ValidateNormalizedStat(definition.Id, nameof(definition.TopSpeed), definition.TopSpeed);
                ValidateNormalizedStat(definition.Id, nameof(definition.Acceleration), definition.Acceleration);
                ValidateNormalizedStat(definition.Id, nameof(definition.Handling), definition.Handling);
                ValidateNormalizedStat(definition.Id, nameof(definition.Drift), definition.Drift);
                ValidateUnlockRequirement(definition.Id, definition.UnlockRequirement);
            }
        }

        public static IReadOnlyList<VehicleDefinition> FilterUnlocked(
            VehicleCatalog catalog,
            VehicleProgressSnapshot progress)
        {
            ValidateOrThrow(catalog);
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var unlocked = new List<VehicleDefinition>();
            foreach (var definition in catalog.Definitions)
            {
                if (IsUnlocked(definition.UnlockRequirement, progress))
                {
                    unlocked.Add(definition);
                }
            }

            return unlocked.AsReadOnly();
        }

        public static bool IsUnlocked(
            VehicleUnlockRequirement requirement,
            VehicleProgressSnapshot progress)
        {
            if (requirement == null)
            {
                throw new ArgumentNullException(nameof(requirement));
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            switch (requirement.Kind)
            {
                case VehicleUnlockKind.Always:
                    return true;
                case VehicleUnlockKind.PlayerLevel:
                    return progress.PlayerLevel >= requirement.Threshold;
                case VehicleUnlockKind.CareerStars:
                    return progress.CareerStars >= requirement.Threshold;
                default:
                    return false;
            }
        }

        public static bool IsTransportSafeId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxTransportIdLength)
            {
                return false;
            }

            if (!IsLowerAlphaNumeric(id[0]))
            {
                return false;
            }

            for (var index = 0; index < id.Length; index++)
            {
                var value = id[index];
                if (IsLowerAlphaNumeric(value) || value == '-' || value == '_' || value == '.')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsLowerAlphaNumeric(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9');
        }

        private static void ValidateNormalizedStat(string id, string statName, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            {
                throw new ArgumentException(
                    $"Vehicle definition '{id}' has invalid normalized stat {statName}={value}; expected a finite value in [0,1].");
            }
        }

        private static void ValidateUnlockRequirement(string id, VehicleUnlockRequirement requirement)
        {
            if (requirement == null)
            {
                throw new ArgumentException($"Vehicle definition '{id}' must provide an unlock requirement.");
            }

            if (!Enum.IsDefined(typeof(VehicleUnlockKind), requirement.Kind))
            {
                throw new ArgumentException(
                    $"Vehicle definition '{id}' has unknown unlock kind {(int)requirement.Kind}.");
            }

            if (requirement.Threshold < 0)
            {
                throw new ArgumentException(
                    $"Vehicle definition '{id}' has a negative unlock threshold {requirement.Threshold}.");
            }

            if (requirement.Kind == VehicleUnlockKind.Always && requirement.Threshold != 0)
            {
                throw new ArgumentException(
                    $"Vehicle definition '{id}' uses Always unlock with non-zero threshold {requirement.Threshold}.");
            }
        }
    }
}
