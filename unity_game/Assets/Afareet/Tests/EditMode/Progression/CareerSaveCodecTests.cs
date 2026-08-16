using System;
using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerSaveCodecTests
    {
        [Test]
        public void EncodeDecode_RoundTripsDeterministicallyAndEscapesIds()
        {
            var codec = new CareerSaveCodec();
            var progress = new CareerProgress(
                CareerProgress.CurrentVersion,
                7,
                new[] { "node_b", "node_\"a\\line\n" },
                new[] { "reward_z", "reward_a" });

            var encoded = codec.Encode(progress);
            var decoded = codec.Decode(encoded);

            Assert.That(encoded, Is.EqualTo(codec.Encode(progress)));
            Assert.That(decoded.Stars, Is.EqualTo(7));
            Assert.That(decoded.CompletedNodeIds, Is.EqualTo(progress.CompletedNodeIds));
            Assert.That(decoded.ClaimedRewardIds, Is.EqualTo(progress.ClaimedRewardIds));
            Assert.That(codec.Encode(decoded), Is.EqualTo(encoded));
        }

        [Test]
        public void Decode_MigratesLegacyV0AndFiltersInvalidListMembers()
        {
            var codec = new CareerSaveCodec();
            var migrated = codec.Decode(
                "{\"totalStars\":12000,\"completed\":[\"legacy_race\",\"\",7,null,\"legacy_race\"]}");

            Assert.That(migrated.Version, Is.EqualTo(CareerProgress.CurrentVersion));
            Assert.That(migrated.Stars, Is.EqualTo(CareerSaveCodec.MaxStoredStars));
            Assert.That(migrated.CompletedNodeIds, Is.EqualTo(new[] { "legacy_race" }));
            Assert.That(migrated.ClaimedRewardIds, Is.Empty);
        }

        [Test]
        public void Decode_CurrentV1ClampsNegativeStarsAndKeepsClaims()
        {
            var codec = new CareerSaveCodec();
            var decoded = codec.Decode(
                "{\"version\":1,\"stars\":-4,\"completedNodeIds\":[\"c01_r01\"],\"claimedRewardIds\":[\"reward_1\"]}");

            Assert.That(decoded.Stars, Is.Zero);
            Assert.That(decoded.CompletedNodeIds, Is.EqualTo(new[] { "c01_r01" }));
            Assert.That(decoded.ClaimedRewardIds, Is.EqualTo(new[] { "reward_1" }));
        }

        [Test]
        public void Encode_RejectsProgressAbovePersistenceBound()
        {
            var codec = new CareerSaveCodec();
            var progress = new CareerProgress(
                CareerProgress.CurrentVersion,
                CareerSaveCodec.MaxStoredStars + 1,
                Array.Empty<string>(),
                Array.Empty<string>());

            Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(progress));
        }

        [Test]
        public void Decode_RejectsMalformedRootsTypesDuplicateKeysAndUnsupportedVersions()
        {
            var codec = new CareerSaveCodec();

            Assert.Throws<FormatException>(() => codec.Decode("[]"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":\"1\"}"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":1,\"stars\":{}}"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":1,\"completedNodeIds\":{}}"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":2}"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":1,\"version\":1}"));
            Assert.Throws<FormatException>(() => codec.Decode("{\"version\":1} trailing"));
        }

        [Test]
        public void Decode_AcceptsExplicitLegacyVersionZero()
        {
            var codec = new CareerSaveCodec();
            var migrated = codec.Decode("{\"version\":0,\"totalStars\":4,\"completed\":[\"legacy_race\"]}");

            Assert.That(migrated.Stars, Is.EqualTo(4));
            Assert.That(migrated.CompletedNodeIds, Is.EqualTo(new[] { "legacy_race" }));
            Assert.That(migrated.ClaimedRewardIds, Is.Empty);
        }
    }
}
