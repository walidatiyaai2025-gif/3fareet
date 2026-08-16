using System;
using System.Linq;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class LiveAiPowerUpBridgePolicyTests
    {
        [Test]
        public void PrototypeRuleset_CoversEveryRetainedPowerUpKind()
        {
            var rules = PowerUpRuntimeDefaults.CreatePrototypeRuleset().Snapshot();

            Assert.That(rules.Count, Is.EqualTo(5));
            CollectionAssert.AreEquivalent(
                Enum.GetValues(typeof(PowerUpKind)).Cast<PowerUpKind>().ToArray(),
                rules.Select(value => value.Kind).ToArray());
            Assert.That(
                rules.Single(value => value.Kind == PowerUpKind.NitroSpirit).InitialCharges,
                Is.EqualTo(2));
            Assert.That(
                rules.Single(value => value.Kind == PowerUpKind.TrafficCurse).TargetMode,
                Is.EqualTo(PowerUpRuntimeTargetMode.Opponent));
        }

        [Test]
        public void SnapshotBuilder_UsesCheckpointSegmentAndRankTelemetry()
        {
            var snapshot = AiPowerUpLiveSnapshotBuilder.Build(
                position: 2,
                fieldSize: 4,
                acceptedCheckpoints: 2,
                checkpointCount: 4,
                segmentProgress: .5d,
                ownSpeedKph: 72d,
                hasTargetAhead: true,
                targetDistanceMeters: 20d,
                targetSpeedKph: 60d,
                hasChaserBehind: true,
                chaserDistanceMeters: 10d,
                incomingHostilePressure: false,
                elapsedRaceSeconds: 50d);

            Assert.That(snapshot.Position, Is.EqualTo(2));
            Assert.That(snapshot.FieldSize, Is.EqualTo(4));
            Assert.That(snapshot.NormalizedProgress, Is.EqualTo(.625d).Within(.0001d));
            Assert.That(snapshot.SpeedRatio, Is.EqualTo(1.2d).Within(.0001d));
            Assert.That(snapshot.GapToTargetSeconds, Is.EqualTo(1d).Within(.0001d));
            Assert.That(snapshot.GapFromChaserSeconds, Is.EqualTo(.5d).Within(.0001d));
            Assert.That(snapshot.RemainingRaceSeconds, Is.EqualTo(30d).Within(.0001d));
        }

        [Test]
        public void SnapshotBuilder_EarlyProgressDoesNotFabricateFinalPushTime()
        {
            var remaining = AiPowerUpLiveSnapshotBuilder.EstimateRemainingRaceSeconds(3d, .02d);

            Assert.That(
                remaining,
                Is.EqualTo(AiPowerUpLiveSnapshotBuilder.UnknownRemainingRaceSeconds));
        }

        [Test]
        public void SnapshotBuilder_InvalidCheckpointTelemetryFailsClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AiPowerUpLiveSnapshotBuilder.Build(
                position: 1,
                fieldSize: 2,
                acceptedCheckpoints: 5,
                checkpointCount: 4,
                segmentProgress: 0d,
                ownSpeedKph: 0d,
                hasTargetAhead: false,
                targetDistanceMeters: 0d,
                targetSpeedKph: 0d,
                hasChaserBehind: false,
                chaserDistanceMeters: 0d,
                incomingHostilePressure: false,
                elapsedRaceSeconds: 0d));
        }
    }
}
