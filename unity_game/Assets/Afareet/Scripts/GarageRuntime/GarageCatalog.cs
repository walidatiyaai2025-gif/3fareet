using System;
using System.Collections.Generic;

namespace Afareet.GarageRuntime
{
    public enum GarageVehicleArchetype
    {
        Hero = 0,
        WedgeCoupe = 1,
        FastbackMuscle = 2,
        CompactPrototype = 3
    }

    public sealed class GarageVehicleStats
    {
        public float TopSpeed { get; }
        public float Acceleration { get; }
        public float Handling { get; }
        public float Drift { get; }
        public float Spirit { get; }

        public GarageVehicleStats(float topSpeed, float acceleration, float handling, float drift, float spirit)
        {
            TopSpeed = Validate(topSpeed, nameof(topSpeed));
            Acceleration = Validate(acceleration, nameof(acceleration));
            Handling = Validate(handling, nameof(handling));
            Drift = Validate(drift, nameof(drift));
            Spirit = Validate(spirit, nameof(spirit));
        }

        private static float Validate(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Garage stats must be finite and non-negative.");
            return value;
        }
    }

    public sealed class GarageNormalizedStats
    {
        public float TopSpeed { get; }
        public float Acceleration { get; }
        public float Handling { get; }
        public float Drift { get; }
        public float Spirit { get; }

        public GarageNormalizedStats(float topSpeed, float acceleration, float handling, float drift, float spirit)
        {
            TopSpeed = Clamp01(topSpeed);
            Acceleration = Clamp01(acceleration);
            Handling = Clamp01(handling);
            Drift = Clamp01(drift);
            Spirit = Clamp01(spirit);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public sealed class GarageCosmeticSet
    {
        private readonly IReadOnlyList<string> paintIds;
        private readonly IReadOnlyList<string> wheelIds;
        private readonly IReadOnlyList<string> trailIds;
        private readonly IReadOnlyList<string> spiritIds;
        private readonly HashSet<string> paintLookup;
        private readonly HashSet<string> wheelLookup;
        private readonly HashSet<string> trailLookup;
        private readonly HashSet<string> spiritLookup;

        public IReadOnlyList<string> PaintIds => paintIds;
        public IReadOnlyList<string> WheelIds => wheelIds;
        public IReadOnlyList<string> TrailIds => trailIds;
        public IReadOnlyList<string> SpiritIds => spiritIds;
        public string DefaultPaintId { get; }
        public string DefaultWheelId { get; }
        public string DefaultTrailId { get; }
        public string DefaultSpiritId { get; }

        public GarageCosmeticSet(
            IEnumerable<string> paintIds,
            IEnumerable<string> wheelIds,
            IEnumerable<string> trailIds,
            IEnumerable<string> spiritIds,
            string defaultPaintId,
            string defaultWheelId,
            string defaultTrailId,
            string defaultSpiritId)
        {
            this.paintIds = NormalizeIds(paintIds, nameof(paintIds), out paintLookup);
            this.wheelIds = NormalizeIds(wheelIds, nameof(wheelIds), out wheelLookup);
            this.trailIds = NormalizeIds(trailIds, nameof(trailIds), out trailLookup);
            this.spiritIds = NormalizeIds(spiritIds, nameof(spiritIds), out spiritLookup);

            DefaultPaintId = RequireMember(defaultPaintId, paintLookup, nameof(defaultPaintId));
            DefaultWheelId = RequireMember(defaultWheelId, wheelLookup, nameof(defaultWheelId));
            DefaultTrailId = RequireMember(defaultTrailId, trailLookup, nameof(defaultTrailId));
            DefaultSpiritId = RequireMember(defaultSpiritId, spiritLookup, nameof(defaultSpiritId));
        }

        public GarageCosmeticSelection CreateDefaultSelection() =>
            new GarageCosmeticSelection(DefaultPaintId, DefaultWheelId, DefaultTrailId, DefaultSpiritId);

        public bool Allows(GarageCosmeticSelection selection)
        {
            if (selection == null) return false;
            return paintLookup.Contains(selection.PaintId) &&
                   wheelLookup.Contains(selection.WheelId) &&
                   trailLookup.Contains(selection.TrailId) &&
                   spiritLookup.Contains(selection.SpiritId);
        }

        public void ValidateOrThrow(GarageCosmeticSelection selection, string vehicleId)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (!Allows(selection))
                throw new InvalidOperationException($"Garage cosmetic selection is not allowed for vehicle '{vehicleId}'.");
        }

        private static IReadOnlyList<string> NormalizeIds(
            IEnumerable<string> source,
            string parameterName,
            out HashSet<string> lookup)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            lookup = new HashSet<string>(StringComparer.Ordinal);
            var ordered = new List<string>();
            foreach (var id in source)
            {
                ValidateId(id, parameterName);
                if (!lookup.Add(id))
                    throw new ArgumentException($"Garage cosmetic id '{id}' is duplicated.", parameterName);
                ordered.Add(id);
            }

            if (ordered.Count == 0)
                throw new ArgumentException("Garage cosmetic option lists must not be empty.", parameterName);
            return ordered.AsReadOnly();
        }

        private static string RequireMember(string id, HashSet<string> lookup, string parameterName)
        {
            ValidateId(id, parameterName);
            if (!lookup.Contains(id))
                throw new ArgumentException($"Garage default cosmetic '{id}' is not present in its option set.", parameterName);
            return id;
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Garage ids must be non-blank.", parameterName);
        }
    }

    public sealed class GarageVehicleDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public GarageVehicleArchetype Archetype { get; }
        public GarageVehicleStats Stats { get; }
        public GarageCosmeticSet Cosmetics { get; }
        public string PreviewResourcePath { get; }

        public GarageVehicleDefinition(
            string id,
            string displayName,
            GarageVehicleArchetype archetype,
            GarageVehicleStats stats,
            GarageCosmeticSet cosmetics,
            string previewResourcePath)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Garage vehicle id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Garage vehicle display name is required.", nameof(displayName));
            if (!Enum.IsDefined(typeof(GarageVehicleArchetype), archetype)) throw new ArgumentOutOfRangeException(nameof(archetype));
            if (string.IsNullOrWhiteSpace(previewResourcePath)) throw new ArgumentException("Garage preview resource path is required.", nameof(previewResourcePath));

            Id = id;
            DisplayName = displayName;
            Archetype = archetype;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            PreviewResourcePath = previewResourcePath;
        }
    }

    public sealed class GarageVehicleAvailability
    {
        public GarageVehicleDefinition Definition { get; }
        public bool IsUnlocked { get; }

        public GarageVehicleAvailability(GarageVehicleDefinition definition, bool isUnlocked)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            IsUnlocked = isUnlocked;
        }
    }

    public sealed class GarageCatalog
    {
        public const int CurrentSchemaVersion = 1;
        public const string StarterVehicleId = "afareet_king";

        private readonly IReadOnlyList<GarageVehicleDefinition> vehicles;
        private readonly Dictionary<string, GarageVehicleDefinition> byId;

        public int SchemaVersion { get; }
        public IReadOnlyList<GarageVehicleDefinition> Vehicles => vehicles;

        public GarageCatalog(int schemaVersion, IEnumerable<GarageVehicleDefinition> vehicles)
        {
            if (schemaVersion != CurrentSchemaVersion)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), $"Unsupported Garage catalog schema {schemaVersion}.");
            if (vehicles == null) throw new ArgumentNullException(nameof(vehicles));

            var ordered = new List<GarageVehicleDefinition>();
            byId = new Dictionary<string, GarageVehicleDefinition>(StringComparer.Ordinal);
            foreach (var definition in vehicles)
            {
                if (definition == null)
                    throw new ArgumentException("Garage catalog cannot contain null vehicle definitions.", nameof(vehicles));
                if (!byId.TryAdd(definition.Id, definition))
                    throw new ArgumentException($"Garage catalog contains duplicate vehicle id '{definition.Id}'.", nameof(vehicles));
                ordered.Add(definition);
            }

            if (ordered.Count == 0)
                throw new ArgumentException("Garage catalog must contain at least one vehicle.", nameof(vehicles));
            if (!byId.ContainsKey(StarterVehicleId))
                throw new ArgumentException($"Garage catalog must contain starter vehicle '{StarterVehicleId}'.", nameof(vehicles));

            SchemaVersion = schemaVersion;
            this.vehicles = ordered.AsReadOnly();
        }

        public bool TryGet(string vehicleId, out GarageVehicleDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                definition = null;
                return false;
            }
            return byId.TryGetValue(vehicleId, out definition);
        }

        public GarageVehicleDefinition GetRequired(string vehicleId)
        {
            if (!TryGet(vehicleId, out var definition))
                throw new KeyNotFoundException($"Unknown Garage vehicle '{vehicleId}'.");
            return definition;
        }

        public IReadOnlyList<GarageVehicleAvailability> GetAvailability(IEnumerable<string> unlockedVehicleIds)
        {
            var unlocked = BuildUnlockLookup(unlockedVehicleIds);
            var result = new List<GarageVehicleAvailability>(vehicles.Count);
            for (var index = 0; index < vehicles.Count; index++)
            {
                var definition = vehicles[index];
                result.Add(new GarageVehicleAvailability(definition, unlocked.Contains(definition.Id)));
            }
            return result.AsReadOnly();
        }

        public IReadOnlyList<GarageVehicleDefinition> GetUnlocked(IEnumerable<string> unlockedVehicleIds)
        {
            var unlocked = BuildUnlockLookup(unlockedVehicleIds);
            var result = new List<GarageVehicleDefinition>();
            for (var index = 0; index < vehicles.Count; index++)
            {
                var definition = vehicles[index];
                if (unlocked.Contains(definition.Id)) result.Add(definition);
            }
            return result.AsReadOnly();
        }

        public GarageNormalizedStats NormalizeStats(string vehicleId)
        {
            var definition = GetRequired(vehicleId);
            return new GarageNormalizedStats(
                Normalize(definition.Stats.TopSpeed, vehicle => vehicle.Stats.TopSpeed),
                Normalize(definition.Stats.Acceleration, vehicle => vehicle.Stats.Acceleration),
                Normalize(definition.Stats.Handling, vehicle => vehicle.Stats.Handling),
                Normalize(definition.Stats.Drift, vehicle => vehicle.Stats.Drift),
                Normalize(definition.Stats.Spirit, vehicle => vehicle.Stats.Spirit));
        }

        public static GarageCatalog CreateDefault()
        {
            return new GarageCatalog(CurrentSchemaVersion, new[]
            {
                CreateVehicle(
                    StarterVehicleId,
                    "Afareet King",
                    GarageVehicleArchetype.Hero,
                    new GarageVehicleStats(86f, 82f, 78f, 88f, 92f),
                    "Art/Vehicles/HeroCar/Production/PF_Vehicle_AfareetKing_Production",
                    "obsidian-purple",
                    "spirit-gold",
                    "purple-spirit",
                    "afareet"),
                CreateVehicle(
                    "wedge_coupe",
                    "Wedge Coupe",
                    GarageVehicleArchetype.WedgeCoupe,
                    new GarageVehicleStats(92f, 88f, 72f, 80f, 68f),
                    "Art/Vehicles/Rivals/Production/PF_Rival_01_Production",
                    "neon-magenta",
                    "razor-gold",
                    "magenta-flare",
                    "wedge-wraith"),
                CreateVehicle(
                    "fastback_muscle",
                    "Fastback Muscle",
                    GarageVehicleArchetype.FastbackMuscle,
                    new GarageVehicleStats(84f, 76f, 66f, 70f, 82f),
                    "Art/Vehicles/Rivals/Production/PF_Rival_02_Production",
                    "ember-orange",
                    "heavy-bronze",
                    "ember-smoke",
                    "iron-djinn"),
                CreateVehicle(
                    "djinn_spirit",
                    "Djinn Spirit",
                    GarageVehicleArchetype.CompactPrototype,
                    new GarageVehicleStats(78f, 94f, 92f, 90f, 86f),
                    "Art/Vehicles/Rivals/Production/PF_Rival_03_Production",
                    "spirit-cyan",
                    "aero-cyan",
                    "cyan-wisp",
                    "djinn-spirit")
            });
        }

        private float Normalize(float value, Func<GarageVehicleDefinition, float> selector)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var index = 0; index < vehicles.Count; index++)
            {
                var candidate = selector(vehicles[index]);
                if (candidate < min) min = candidate;
                if (candidate > max) max = candidate;
            }
            if (max <= min) return 1f;
            return (value - min) / (max - min);
        }

        private HashSet<string> BuildUnlockLookup(IEnumerable<string> unlockedVehicleIds)
        {
            var unlocked = new HashSet<string>(StringComparer.Ordinal) { StarterVehicleId };
            if (unlockedVehicleIds == null) return unlocked;

            foreach (var id in unlockedVehicleIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Garage unlock ids must be non-blank.", nameof(unlockedVehicleIds));
                if (!byId.ContainsKey(id))
                    throw new ArgumentException($"Garage unlock id '{id}' is not present in the catalog.", nameof(unlockedVehicleIds));
                unlocked.Add(id);
            }
            return unlocked;
        }

        private static GarageVehicleDefinition CreateVehicle(
            string id,
            string displayName,
            GarageVehicleArchetype archetype,
            GarageVehicleStats stats,
            string previewResourcePath,
            string defaultPaint,
            string defaultWheel,
            string defaultTrail,
            string defaultSpirit)
        {
            var cosmetics = new GarageCosmeticSet(
                new[] { defaultPaint, "obsidian", "spirit-white" },
                new[] { defaultWheel, "shadow-rim", "spirit-ring" },
                new[] { defaultTrail, "purple-wisp", "gold-spark" },
                new[] { defaultSpirit, "afareet", "djinn" },
                defaultPaint,
                defaultWheel,
                defaultTrail,
                defaultSpirit);

            return new GarageVehicleDefinition(id, displayName, archetype, stats, cosmetics, previewResourcePath);
        }
    }
}
