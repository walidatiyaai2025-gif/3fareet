using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Afareet.Progression;

namespace Afareet.CareerRuntime
{
    public sealed class CareerPlayerProfile
    {
        private readonly IReadOnlyList<string> unlockedVehicleIds;
        private readonly HashSet<string> unlockedVehicleLookup;

        public CareerProgress Career { get; }
        public int Coins { get; }
        public int Spirit { get; }
        public IReadOnlyList<string> UnlockedVehicleIds => unlockedVehicleIds;

        public CareerPlayerProfile(
            CareerProgress career,
            int coins,
            int spirit,
            IEnumerable<string> unlockedVehicleIds)
        {
            Career = career ?? throw new ArgumentNullException(nameof(career));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            if (spirit < 0) throw new ArgumentOutOfRangeException(nameof(spirit));
            if (unlockedVehicleIds == null) throw new ArgumentNullException(nameof(unlockedVehicleIds));

            Coins = coins;
            Spirit = spirit;
            unlockedVehicleLookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in unlockedVehicleIds)
            {
                ValidateId(id, nameof(unlockedVehicleIds));
                unlockedVehicleLookup.Add(id);
            }

            var ordered = new List<string>(unlockedVehicleLookup);
            ordered.Sort(StringComparer.Ordinal);
            this.unlockedVehicleIds = ordered.AsReadOnly();
        }

        public static CareerPlayerProfile Empty() =>
            new CareerPlayerProfile(CareerProgress.Empty(), 0, 0, Array.Empty<string>());

        public bool IsVehicleUnlocked(string vehicleId)
        {
            ValidateId(vehicleId, nameof(vehicleId));
            return unlockedVehicleLookup.Contains(vehicleId);
        }

        public CareerPlayerProfile Apply(CareerEventSettlement settlement)
        {
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));

            int nextCoins;
            int nextSpirit;
            checked
            {
                nextCoins = Coins + settlement.CoinsGranted;
                nextSpirit = Spirit + settlement.SpiritGranted;
            }

            var vehicles = new List<string>(unlockedVehicleIds);
            for (var index = 0; index < settlement.UnlockedVehicleIds.Count; index++)
                vehicles.Add(settlement.UnlockedVehicleIds[index]);

            return new CareerPlayerProfile(
                settlement.Progress,
                nextCoins,
                nextSpirit,
                vehicles);
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Career profile ids must be non-blank.", parameterName);
        }
    }

    public sealed class CareerPlayerProfileCodec
    {
        private const string Header = "AFAREET_PROFILE_V1";
        private readonly CareerSaveCodec careerCodec = new CareerSaveCodec();

        public string Encode(CareerPlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var builder = new StringBuilder();
            builder.AppendLine(Header);
            builder.AppendLine(ToBase64(careerCodec.Encode(profile.Career)));
            builder.AppendLine(profile.Coins.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(profile.Spirit.ToString(CultureInfo.InvariantCulture));
            for (var index = 0; index < profile.UnlockedVehicleIds.Count; index++)
            {
                if (index > 0) builder.Append(',');
                builder.Append(ToBase64(profile.UnlockedVehicleIds[index]));
            }
            return builder.ToString();
        }

        public CareerPlayerProfile Decode(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var lines = source.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 5 || !StringComparer.Ordinal.Equals(lines[0], Header))
                throw new FormatException("Unsupported Career player profile format.");

            var career = careerCodec.Decode(FromBase64(lines[1], "career"));
            int coins;
            int spirit;
            if (!int.TryParse(lines[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out coins) || coins < 0)
                throw new FormatException("Career player profile coins must be a non-negative integer.");
            if (!int.TryParse(lines[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out spirit) || spirit < 0)
                throw new FormatException("Career player profile spirit must be a non-negative integer.");

            var vehicles = new List<string>();
            if (!string.IsNullOrWhiteSpace(lines[4]))
            {
                var encodedVehicles = lines[4].Split(',');
                for (var index = 0; index < encodedVehicles.Length; index++)
                    vehicles.Add(FromBase64(encodedVehicles[index], "vehicle"));
            }

            return new CareerPlayerProfile(career, coins, spirit, vehicles);
        }

        private static string ToBase64(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        private static string FromBase64(string value, string field)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch (FormatException exception)
            {
                throw new FormatException($"Career player profile {field} payload is invalid base64.", exception);
            }
        }
    }

    public sealed class CareerPlayerProfileLoadResult
    {
        public CareerPlayerProfile Profile { get; }
        public bool HasStoredPayload { get; }
        public bool RecoveredFromInvalidPayload { get; }
        public bool MigratedLegacyCareerSave { get; }
        public string Error { get; }

        public CareerPlayerProfileLoadResult(
            CareerPlayerProfile profile,
            bool hasStoredPayload,
            bool recoveredFromInvalidPayload,
            bool migratedLegacyCareerSave = false,
            string error = null)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            HasStoredPayload = hasStoredPayload;
            RecoveredFromInvalidPayload = recoveredFromInvalidPayload;
            MigratedLegacyCareerSave = migratedLegacyCareerSave;
            Error = error;
        }
    }

    public sealed class CareerPlayerProfileStore
    {
        private readonly ICareerProgressStorage storage;
        private readonly CareerPlayerProfileCodec codec = new CareerPlayerProfileCodec();
        private readonly CareerSaveCodec legacyCareerCodec = new CareerSaveCodec();

        public CareerPlayerProfileStore(ICareerProgressStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public CareerPlayerProfileLoadResult Load()
        {
            string payload;
            if (!storage.TryRead(out payload) || string.IsNullOrWhiteSpace(payload))
                return new CareerPlayerProfileLoadResult(CareerPlayerProfile.Empty(), false, false);

            try
            {
                return new CareerPlayerProfileLoadResult(codec.Decode(payload), true, false);
            }
            catch (Exception profileException) when (
                profileException is FormatException ||
                profileException is ArgumentException ||
                profileException is OverflowException)
            {
                try
                {
                    var legacyCareer = legacyCareerCodec.Decode(payload);
                    return new CareerPlayerProfileLoadResult(
                        new CareerPlayerProfile(legacyCareer, 0, 0, Array.Empty<string>()),
                        true,
                        false,
                        migratedLegacyCareerSave: true);
                }
                catch (Exception legacyException) when (
                    legacyException is FormatException ||
                    legacyException is ArgumentException ||
                    legacyException is OverflowException)
                {
                    return new CareerPlayerProfileLoadResult(
                        CareerPlayerProfile.Empty(),
                        true,
                        true,
                        error: profileException.Message);
                }
            }
        }

        public void Save(CareerPlayerProfile profile)
        {
            storage.Write(codec.Encode(profile ?? throw new ArgumentNullException(nameof(profile))));
        }
    }
}
