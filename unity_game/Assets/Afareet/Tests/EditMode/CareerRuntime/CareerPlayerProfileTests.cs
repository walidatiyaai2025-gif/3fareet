using System;
using Afareet.CareerRuntime;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.CareerRuntime
{
    public sealed class CareerPlayerProfileTests
    {
        [Test]
        public void Codec_RoundTripsCareerWalletAndUnlocksDeterministically()
        {
            var career = new CareerProgress(
                CareerProgress.CurrentVersion,
                9,
                new[] { "c01_r03", "c01_r01", "c01_r02" },
                new[] { "career:c01_r01:reward:00" });
            var profile = new CareerPlayerProfile(
                career,
                1250,
                44,
                new[] { "djinn_spirit", "afareet_king" });
            var codec = new CareerPlayerProfileCodec();

            var encoded = codec.Encode(profile);
            var decoded = codec.Decode(encoded);

            Assert.That(decoded.Career.Stars, Is.EqualTo(9));
            Assert.That(decoded.Career.IsNodeCompleted("c01_r03"), Is.True);
            Assert.That(decoded.Coins, Is.EqualTo(1250));
            Assert.That(decoded.Spirit, Is.EqualTo(44));
            Assert.That(decoded.UnlockedVehicleIds, Is.EqualTo(new[] { "afareet_king", "djinn_spirit" }));
            Assert.That(decoded.IsVehicleUnlocked("djinn_spirit"), Is.True);
            Assert.That(codec.Encode(decoded), Is.EqualTo(encoded));
        }

        [Test]
        public void Store_MigratesLegacyCareerSaveWithoutInventingWallet()
        {
            var legacy = new CareerProgress(
                CareerProgress.CurrentVersion,
                3,
                new[] { "c01_r01" },
                new[] { "career:c01_r01:reward:00" });
            var storage = new MemoryStorage
            {
                Payload = new CareerSaveCodec().Encode(legacy)
            };

            var result = new CareerPlayerProfileStore(storage).Load();

            Assert.That(result.RecoveredFromInvalidPayload, Is.False);
            Assert.That(result.MigratedLegacyCareerSave, Is.True);
            Assert.That(result.Profile.Career.Stars, Is.EqualTo(3));
            Assert.That(result.Profile.Career.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(result.Profile.Coins, Is.Zero);
            Assert.That(result.Profile.Spirit, Is.Zero);
            Assert.That(result.Profile.UnlockedVehicleIds, Is.Empty);
        }

        [Test]
        public void ApplySettlement_AddsGrantedWalletAndVehicleExactlyOncePerSettlement()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[4];
            var settlement = new CareerEventSettlementService().Settle(
                definition,
                new CareerEventOutcome(true, 0, finalPosition: 1),
                CareerProgress.Empty());
            var profile = CareerPlayerProfile.Empty().Apply(settlement);

            Assert.That(profile.Coins, Is.EqualTo(650));
            Assert.That(profile.Spirit, Is.EqualTo(9));
            Assert.That(profile.IsVehicleUnlocked("djinn_spirit"), Is.True);
            Assert.That(profile.Career.IsNodeCompleted("c01_boss"), Is.True);
            Assert.That(profile.Career.IsRewardClaimed("career:c01_boss:reward:00"), Is.True);
            Assert.That(profile.Career.IsRewardClaimed("career:c01_boss:reward:01"), Is.True);
        }

        [Test]
        public void Store_InvalidProfileFailsSafeWithoutDeletingOriginalPayload()
        {
            var storage = new MemoryStorage { Payload = "broken-profile" };
            var result = new CareerPlayerProfileStore(storage).Load();

            Assert.That(result.RecoveredFromInvalidPayload, Is.True);
            Assert.That(result.MigratedLegacyCareerSave, Is.False);
            Assert.That(result.Profile.Career.Stars, Is.Zero);
            Assert.That(result.Profile.Coins, Is.Zero);
            Assert.That(storage.Payload, Is.EqualTo("broken-profile"));
        }

        [Test]
        public void Constructor_RejectsNegativeBalancesAndBlankUnlockIds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CareerPlayerProfile(CareerProgress.Empty(), -1, 0, Array.Empty<string>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CareerPlayerProfile(CareerProgress.Empty(), 0, -1, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() =>
                new CareerPlayerProfile(CareerProgress.Empty(), 0, 0, new[] { " " }));
        }

        private sealed class MemoryStorage : ICareerProgressStorage
        {
            public string Payload;
            public bool TryRead(out string payload)
            {
                payload = Payload;
                return Payload != null;
            }
            public void Write(string payload) => Payload = payload;
            public void Clear() => Payload = null;
        }
    }
}
