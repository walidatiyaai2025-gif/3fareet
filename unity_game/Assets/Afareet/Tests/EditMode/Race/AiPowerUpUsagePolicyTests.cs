using System;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class AiPowerUpUsagePolicyTests
    {
        [Test]
        public void Decide_IsDeterministicForIdenticalInputs()
        {
            var snapshot = Snapshot(position: 3, hasTargetAhead: true, gapToTarget: 1.5d, speedRatio: 0.91d);
            var inventory = new[]
            {
                Available(PowerUpKind.NitroSpirit),
                Available(PowerUpKind.TrafficCurse)
            };

            var first = AiPowerUpUsagePolicy.Decide(snapshot, inventory);
            var second = AiPowerUpUsagePolicy.Decide(snapshot, inventory);

            Assert.That(first.Kind, Is.EqualTo(PowerUpKind.NitroSpirit));
            Assert.That(second.Kind, Is.EqualTo(first.Kind));
            Assert.That(second.Reason, Is.EqualTo(first.Reason));
        }

        [Test]
        public void Decide_EyeShieldHasDefensivePrecedence()
        {
            var snapshot = Snapshot(
                position: 2,
                incomingHostilePressure: true,
                hasChaserBehind: true,
                gapFromChaser: 0.5d,
                hasTargetAhead: true,
                gapToTarget: 1d,
                speedRatio: 0.8d);
            var inventory = new[]
            {
                Available(PowerUpKind.EyeShield),
                Available(PowerUpKind.AsphaltShard),
                Available(PowerUpKind.NitroSpirit),
                Available(PowerUpKind.TrafficCurse)
            };

            var decision = AiPowerUpUsagePolicy.Decide(snapshot, inventory);

            Assert.That(decision.Kind, Is.EqualTo(PowerUpKind.EyeShield));
            Assert.That(decision.Reason, Is.EqualTo(AiPowerUpDecisionReason.DefensiveShield));
        }

        [Test]
        public void Decide_DoesNotSelectCooldownOrEmptyInventoryEntry()
        {
            var snapshot = Snapshot(position: 3, hasTargetAhead: true, gapToTarget: 1d, speedRatio: 0.8d);
            var inventory = new[]
            {
                new AiPowerUpAvailability(PowerUpKind.NitroSpirit, charges: 1, cooldownRemainingSeconds: 2d),
                new AiPowerUpAvailability(PowerUpKind.TrafficCurse, charges: 0, cooldownRemainingSeconds: 0d)
            };

            var decision = AiPowerUpUsagePolicy.Decide(snapshot, inventory);

            Assert.That(decision.ShouldUse, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(AiPowerUpDecisionReason.None));
        }

        [Test]
        public void Decide_TrafficCurseRequiresValidTargetInRange()
        {
            var inventory = new[] { Available(PowerUpKind.TrafficCurse) };

            Assert.That(
                AiPowerUpUsagePolicy.Decide(
                    Snapshot(position: 2, hasTargetAhead: false, gapToTarget: 0d),
                    inventory).ShouldUse,
                Is.False);

            Assert.That(
                AiPowerUpUsagePolicy.Decide(
                    Snapshot(position: 2, hasTargetAhead: true, gapToTarget: 3d),
                    inventory).ShouldUse,
                Is.False);

            var inRange = AiPowerUpUsagePolicy.Decide(
                Snapshot(position: 2, hasTargetAhead: true, gapToTarget: 2d),
                inventory);
            Assert.That(inRange.Kind, Is.EqualTo(PowerUpKind.TrafficCurse));
        }

        [Test]
        public void Decide_NitroHandlesCatchUpAndFinalPush()
        {
            var inventory = new[] { Available(PowerUpKind.NitroSpirit) };

            var catchUp = AiPowerUpUsagePolicy.Decide(
                Snapshot(position: 4, hasTargetAhead: true, gapToTarget: 2d, speedRatio: 1d),
                inventory);
            Assert.That(catchUp.Kind, Is.EqualTo(PowerUpKind.NitroSpirit));

            var finalPush = AiPowerUpUsagePolicy.Decide(
                Snapshot(position: 2, normalizedProgress: 0.9d, hasTargetAhead: true, gapToTarget: 0.2d, speedRatio: 1.05d),
                inventory);
            Assert.That(finalPush.Kind, Is.EqualTo(PowerUpKind.NitroSpirit));
        }

        [Test]
        public void Decide_AsphaltShardDefendsAgainstCloseChaser()
        {
            var decision = AiPowerUpUsagePolicy.Decide(
                Snapshot(position: 1, hasChaserBehind: true, gapFromChaser: 0.8d),
                new[] { Available(PowerUpKind.AsphaltShard) });

            Assert.That(decision.Kind, Is.EqualTo(PowerUpKind.AsphaltShard));
            Assert.That(decision.Reason, Is.EqualTo(AiPowerUpDecisionReason.DefendFromChaser));
        }

        [Test]
        public void Decide_EnchantedPoundNeverOutranksDefense()
        {
            var snapshot = Snapshot(
                position: 1,
                normalizedProgress: 0.8d,
                hasChaserBehind: true,
                gapFromChaser: 0.7d);
            var inventory = new[]
            {
                Available(PowerUpKind.EnchantedPound),
                Available(PowerUpKind.AsphaltShard)
            };

            var decision = AiPowerUpUsagePolicy.Decide(snapshot, inventory);

            Assert.That(decision.Kind, Is.EqualTo(PowerUpKind.AsphaltShard));
        }

        [Test]
        public void Decide_EnchantedPoundRequiresStableLateRaceLead()
        {
            var inventory = new[] { Available(PowerUpKind.EnchantedPound) };

            Assert.That(
                AiPowerUpUsagePolicy.Decide(
                    Snapshot(position: 1, normalizedProgress: 0.5d, hasChaserBehind: true, gapFromChaser: 3d),
                    inventory).ShouldUse,
                Is.False);

            var decision = AiPowerUpUsagePolicy.Decide(
                Snapshot(position: 1, normalizedProgress: 0.75d, hasChaserBehind: true, gapFromChaser: 3d),
                inventory);
            Assert.That(decision.Kind, Is.EqualTo(PowerUpKind.EnchantedPound));
            Assert.That(decision.Reason, Is.EqualTo(AiPowerUpDecisionReason.RewardOptimization));
        }

        [Test]
        public void Decide_RejectsDuplicateInventoryKinds()
        {
            var inventory = new[]
            {
                Available(PowerUpKind.NitroSpirit),
                Available(PowerUpKind.NitroSpirit)
            };

            Assert.Throws<ArgumentException>(() =>
                AiPowerUpUsagePolicy.Decide(Snapshot(position: 2), inventory));
        }

        private static AiPowerUpAvailability Available(PowerUpKind kind)
        {
            return new AiPowerUpAvailability(kind, charges: 1, cooldownRemainingSeconds: 0d);
        }

        private static AiPowerUpRaceSnapshot Snapshot(
            int position,
            int fieldSize = 6,
            double normalizedProgress = 0.5d,
            double speedRatio = 1d,
            bool hasTargetAhead = false,
            double gapToTarget = 0d,
            bool hasChaserBehind = false,
            double gapFromChaser = 0d,
            bool incomingHostilePressure = false,
            double remainingRaceSeconds = 60d)
        {
            return new AiPowerUpRaceSnapshot(
                position,
                fieldSize,
                normalizedProgress,
                speedRatio,
                hasTargetAhead,
                gapToTarget,
                hasChaserBehind,
                gapFromChaser,
                incomingHostilePressure,
                remainingRaceSeconds);
        }
    }
}
