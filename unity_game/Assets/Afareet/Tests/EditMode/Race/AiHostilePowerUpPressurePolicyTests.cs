using System;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class AiHostilePowerUpPressurePolicyTests
    {
        [Test]
        public void CloseLeaderWithUsableShardCreatesPressure()
        {
            var pressure = AiHostilePowerUpPressurePolicy.HasIncomingPressure(
                ownSpeedKph: 72d,
                leaderAheadCanUseAsphaltShard: true,
                leaderAheadDistanceMeters: 20d,
                chaserBehindCanUseTrafficCurse: false,
                chaserBehindDistanceMeters: 0d);

            Assert.That(pressure, Is.True);
        }

        [Test]
        public void CloseChaserWithUsableCurseCreatesPressure()
        {
            var pressure = AiHostilePowerUpPressurePolicy.HasIncomingPressure(
                ownSpeedKph: 72d,
                leaderAheadCanUseAsphaltShard: false,
                leaderAheadDistanceMeters: 0d,
                chaserBehindCanUseTrafficCurse: true,
                chaserBehindDistanceMeters: 40d);

            Assert.That(pressure, Is.True);
        }

        [Test]
        public void ThreatOutsidePolicyRangeDoesNotWasteShield()
        {
            var pressure = AiHostilePowerUpPressurePolicy.HasIncomingPressure(
                ownSpeedKph: 72d,
                leaderAheadCanUseAsphaltShard: true,
                leaderAheadDistanceMeters: 80d,
                chaserBehindCanUseTrafficCurse: true,
                chaserBehindDistanceMeters: 80d);

            Assert.That(pressure, Is.False);
        }

        [Test]
        public void NearbyRacersWithoutUsableHostileInventoryDoNotCreatePressure()
        {
            var pressure = AiHostilePowerUpPressurePolicy.HasIncomingPressure(
                ownSpeedKph: 72d,
                leaderAheadCanUseAsphaltShard: false,
                leaderAheadDistanceMeters: 5d,
                chaserBehindCanUseTrafficCurse: false,
                chaserBehindDistanceMeters: 5d);

            Assert.That(pressure, Is.False);
        }

        [Test]
        public void InventoryUsabilityRequiresChargeAndNoCooldown()
        {
            var ready = new[]
            {
                new PowerUpInventorySnapshot(PowerUpKind.AsphaltShard, 1, 0d),
                new PowerUpInventorySnapshot(PowerUpKind.TrafficCurse, 0, 0d)
            };
            var cooldown = new[]
            {
                new PowerUpInventorySnapshot(PowerUpKind.AsphaltShard, 1, 1.5d)
            };

            Assert.That(AiHostilePowerUpPressurePolicy.IsUsable(ready, PowerUpKind.AsphaltShard), Is.True);
            Assert.That(AiHostilePowerUpPressurePolicy.IsUsable(ready, PowerUpKind.TrafficCurse), Is.False);
            Assert.That(AiHostilePowerUpPressurePolicy.IsUsable(cooldown, PowerUpKind.AsphaltShard), Is.False);
        }

        [Test]
        public void InvalidMetricsFailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AiHostilePowerUpPressurePolicy.HasIncomingPressure(-1d, false, 0d, false, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AiHostilePowerUpPressurePolicy.HasIncomingPressure(0d, false, double.NaN, false, 0d));
        }
    }
}
