using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Afareet.GarageRuntime
{
    public interface IGarageStateStorage
    {
        bool TryRead(out string payload);
        void Write(string payload);
    }

    public sealed class GarageStateDecodeResult
    {
        public GarageState State { get; }
        public bool MigratedLegacyV1 { get; }

        public GarageStateDecodeResult(GarageState state, bool migratedLegacyV1)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            MigratedLegacyV1 = migratedLegacyV1;
        }
    }

    public sealed class GarageStateCodec
    {
        public const string CurrentHeader = "AFAREET_GARAGE_V2";
        public const string LegacyHeaderV1 = "AFAREET_GARAGE_V1";

        public string Encode(GarageState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var builder = new StringBuilder();
            builder.AppendLine(CurrentHeader);
            builder.AppendLine(state.EquippedVehicleId == null ? string.Empty : ToBase64(state.EquippedVehicleId));

            var ordered = new List<KeyValuePair<string, GarageCosmeticSelection>>(state.Selections);
            ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
            builder.AppendLine(ordered.Count.ToString(CultureInfo.InvariantCulture));
            for (var index = 0; index < ordered.Count; index++)
            {
                var pair = ordered[index];
                var selection = pair.Value;
                builder.Append(ToBase64(pair.Key));
                builder.Append('|');
                builder.Append(ToBase64(selection.PaintId));
                builder.Append('|');
                builder.Append(ToBase64(selection.WheelId));
                builder.Append('|');
                builder.Append(ToBase64(selection.TrailId));
                builder.Append('|');
                builder.AppendLine(ToBase64(selection.SpiritId));
            }
            return builder.ToString();
        }

        public GarageStateDecodeResult Decode(string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var lines = payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (lines.Length == 0) throw new FormatException("Garage save payload is empty.");

            if (StringComparer.Ordinal.Equals(lines[0], LegacyHeaderV1))
                return DecodeLegacyV1(lines);
            if (!StringComparer.Ordinal.Equals(lines[0], CurrentHeader))
                throw new FormatException($"Unsupported Garage save header '{lines[0]}'.");
            if (lines.Length < 3)
                throw new FormatException("Garage V2 save is truncated.");

            var equipped = string.IsNullOrWhiteSpace(lines[1]) ? null : FromBase64(lines[1], "equipped vehicle");
            if (!int.TryParse(lines[2], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 0)
                throw new FormatException("Garage V2 selection count must be a non-negative integer.");
            if (lines.Length < 3 + count)
                throw new FormatException("Garage V2 selection payload is truncated.");

            var selections = new List<KeyValuePair<string, GarageCosmeticSelection>>(count);
            for (var index = 0; index < count; index++)
            {
                var line = lines[3 + index];
                var fields = line.Split('|');
                if (fields.Length != 5)
                    throw new FormatException($"Garage V2 selection line {index} must contain exactly five fields.");

                var vehicleId = FromBase64(fields[0], $"selection {index} vehicle");
                var selection = new GarageCosmeticSelection(
                    FromBase64(fields[1], $"selection {index} paint"),
                    FromBase64(fields[2], $"selection {index} wheel"),
                    FromBase64(fields[3], $"selection {index} trail"),
                    FromBase64(fields[4], $"selection {index} spirit"));
                selections.Add(new KeyValuePair<string, GarageCosmeticSelection>(vehicleId, selection));
            }

            return new GarageStateDecodeResult(new GarageState(equipped, selections), false);
        }

        public string EncodeLegacyV1ForMigrationFixture(string equippedVehicleId)
        {
            if (equippedVehicleId != null && string.IsNullOrWhiteSpace(equippedVehicleId))
                throw new ArgumentException("Legacy Garage equipped vehicle id must be null or non-blank.", nameof(equippedVehicleId));
            return LegacyHeaderV1 + "\n" + (equippedVehicleId == null ? string.Empty : ToBase64(equippedVehicleId)) + "\n";
        }

        private static GarageStateDecodeResult DecodeLegacyV1(string[] lines)
        {
            if (lines.Length < 2)
                throw new FormatException("Garage V1 save is truncated.");
            var equipped = string.IsNullOrWhiteSpace(lines[1]) ? null : FromBase64(lines[1], "legacy equipped vehicle");
            return new GarageStateDecodeResult(new GarageState(equipped), true);
        }

        private static string ToBase64(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string FromBase64(string value, string field)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                if (string.IsNullOrWhiteSpace(decoded))
                    throw new FormatException($"Garage {field} must not decode to a blank id.");
                return decoded;
            }
            catch (FormatException exception)
            {
                throw new FormatException($"Garage {field} is invalid base64 or blank.", exception);
            }
        }
    }

    public sealed class GarageStateLoadResult
    {
        public GarageState State { get; }
        public bool HasStoredPayload { get; }
        public bool MigratedLegacyV1 { get; }
        public bool RecoveredFromInvalidPayload { get; }
        public bool RewrittenCanonicalPayload { get; }
        public string Error { get; }

        public GarageStateLoadResult(
            GarageState state,
            bool hasStoredPayload,
            bool migratedLegacyV1,
            bool recoveredFromInvalidPayload,
            bool rewrittenCanonicalPayload,
            string error = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            HasStoredPayload = hasStoredPayload;
            MigratedLegacyV1 = migratedLegacyV1;
            RecoveredFromInvalidPayload = recoveredFromInvalidPayload;
            RewrittenCanonicalPayload = rewrittenCanonicalPayload;
            Error = error;
        }
    }

    public sealed class GarageStateStore
    {
        private readonly IGarageStateStorage storage;
        private readonly GarageCatalog catalog;
        private readonly GarageStateCodec codec = new GarageStateCodec();

        public GarageStateStore(IGarageStateStorage storage, GarageCatalog catalog)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public GarageStateLoadResult Load(IEnumerable<string> unlockedVehicleIds = null)
        {
            if (!storage.TryRead(out var payload) || string.IsNullOrWhiteSpace(payload))
            {
                var defaultState = new GarageService(catalog, unlockedVehicleIds).State;
                return new GarageStateLoadResult(defaultState, false, false, false, false);
            }

            try
            {
                var decoded = codec.Decode(payload);
                var normalized = new GarageService(catalog, unlockedVehicleIds, decoded.State).State;
                var canonical = codec.Encode(normalized);
                var rewritten = decoded.MigratedLegacyV1 || !PayloadEquivalent(payload, canonical);
                if (rewritten) storage.Write(canonical);
                return new GarageStateLoadResult(
                    normalized,
                    true,
                    decoded.MigratedLegacyV1,
                    false,
                    rewritten);
            }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is KeyNotFoundException ||
                exception is OverflowException)
            {
                var recovered = new GarageService(catalog, unlockedVehicleIds).State;
                storage.Write(codec.Encode(recovered));
                return new GarageStateLoadResult(
                    recovered,
                    true,
                    false,
                    true,
                    true,
                    exception.Message);
            }
        }

        public void Save(GarageState state, IEnumerable<string> unlockedVehicleIds = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var normalized = new GarageService(catalog, unlockedVehicleIds, state).State;
            storage.Write(codec.Encode(normalized));
        }

        private static bool PayloadEquivalent(string left, string right)
        {
            return StringComparer.Ordinal.Equals(NormalizePayload(left), NormalizePayload(right));
        }

        private static string NormalizePayload(string value)
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .TrimEnd('\n');
        }
    }
}
