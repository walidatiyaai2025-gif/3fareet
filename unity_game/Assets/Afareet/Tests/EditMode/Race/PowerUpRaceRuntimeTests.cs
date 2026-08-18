using System.Collections.Generic;
using System.Linq;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class PowerUpRaceRuntimeTests
    {
        [Test]
        public void TryUse_ConsumesChargeAndEnforcesCooldownFromOneAuthoritativeSlot()
        {
            var runtime = Runtime(Rules(nitroCharges: 2, nitroCooldown: 5d));

            var first = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
            var duringCooldown = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 1d);
            var afterCooldown = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 5d);

            Assert.That(first.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(first.RemainingCharges, Is.EqualTo(1));
            Assert.That(first.CooldownRemainingSeconds, Is.EqualTo(5d));
            Assert.That(duringCooldown.Status, Is.EqualTo(PowerUpRuntimeUseStatus.CooldownActive));
            Assert.That(duringCooldown.RemainingCharges, Is.EqualTo(1));
            Assert.That(duringCooldown.CooldownRemainingSeconds, Is.EqualTo(4d));
            Assert.That(afterCooldown.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(afterCooldown.RemainingCharges, Is.Zero);
        }

        [Test]
        public void HostileUseBlockedByEyeShield_IsStillConsumedAsRealAttempt()
        {
            var runtime = Runtime(Rules(eyeShieldCharges: 1, trafficCharges: 2));

            runtime.TryUse("player", PowerUpKind.EyeShield, null, 0d);
            var blocked = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "player", .1d);

            Assert.That(blocked.Status, Is.EqualTo(PowerUpRuntimeUseStatus.BlockedByEyeShield));
            Assert.That(blocked.Consumed, Is.True);
            Assert.That(blocked.EffectResult, Is.EqualTo(PowerUpApplyResult.BlockedByEyeShield));
            Assert.That(blocked.RemainingCharges, Is.EqualTo(1));
            Assert.That(runtime.GetActiveEffect("player", PowerUpKind.TrafficCurse, .1d), Is.Null);
        }

        [Test]
        public void IgnoredEffectPolicy_DoesNotConsumeChargeOrStartCooldown()
        {
            var runtime = Runtime(Rules(enchantedCharges: 2, enchantedCooldown: 4d));

            var first = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 0d);
            var ignored = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 4d);

            Assert.That(first.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(first.RemainingCharges, Is.EqualTo(1));
            Assert.That(ignored.Status, Is.EqualTo(PowerUpRuntimeUseStatus.IgnoredByEffectPolicy));
            Assert.That(ignored.Consumed, Is.False);
            Assert.That(ignored.RemainingCharges, Is.EqualTo(1));
            Assert.That(ignored.CooldownRemainingSeconds, Is.Zero);
        }

        [Test]
        public void AiAvailabilityAndExecution_UseTheSameInventoryAndTryUsePath()
        {
            var runtime = Runtime(Rules(eyeShieldCharges: 1));
            var snapshot = new AiPowerUpRaceSnapshot(
                position: 2,
                fieldSize: 3,
                normalizedProgress: .4d,
                speedRatio: 1d,
                hasTargetAhead: true,
                gapToTargetSeconds: 1d,
                hasChaserBehind: false,
                gapFromChaserSeconds: 0d,
                incomingHostilePressure: true,
                remainingRaceSeconds: 30d);

            var before = runtime.GetAiAvailability("rival-a", 0d)
                .Single(value => value.Kind == PowerUpKind.EyeShield);
            var execution = runtime.ExecuteAiDecision("rival-a", snapshot, "player", null, 0d);
            var after = runtime.GetAiAvailability("rival-a", 0d)
                .Single(value => value.Kind == PowerUpKind.EyeShield);

            Assert.That(before.IsUsable, Is.True);
            Assert.That(execution.Decision.Kind, Is.EqualTo(PowerUpKind.EyeShield));
            Assert.That(execution.Decision.Reason, Is.EqualTo(AiPowerUpDecisionReason.DefensiveShield));
            Assert.That(execution.UseResult.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(after.Charges, Is.Zero);
            Assert.That(runtime.GetActiveEffect("rival-a", PowerUpKind.EyeShield, 0d), Is.Not.Null);
        }

        [Test]
        public void TargetedAiDecision_UsesCallerSuppliedDeterministicOpponent()
        {
            var runtime = Runtime(Rules(trafficCharges: 1));
            var snapshot = new AiPowerUpRaceSnapshot(
                position: 2,
                fieldSize: 3,
                normalizedProgress: .5d,
                speedRatio: 1.05d,
                hasTargetAhead: true,
                gapToTargetSeconds: 1d,
                hasChaserBehind: false,
                gapFromChaserSeconds: 0d,
                incomingHostilePressure: false,
                remainingRaceSeconds: 40d);

            var execution = runtime.ExecuteAiDecision("rival-a", snapshot, "player", null, 0d);

            Assert.That(execution.Decision.Kind, Is.EqualTo(PowerUpKind.TrafficCurse));
            Assert.That(execution.UseResult.TargetRacerId, Is.EqualTo("player"));
            Assert.That(runtime.GetActiveEffect("player", PowerUpKind.TrafficCurse, 0d), Is.Not.Null);
        }

        [Test]
        public void TickAll_IsStableByRacerId_AndResetRestoresRaceScopedState()
        {
            var runtime = Runtime(Rules(nitroCharges: 2, effectDuration: 1d));

            runtime.TryUse("rival-a", PowerUpKind.NitroSpirit, null, 0d);
            runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
            var tick = runtime.TickAll(2d);

            CollectionAssert.AreEqual(
                new[] { "player", "rival-a", "rival-b" },
                tick.Select(value => value.RacerId).ToArray());
            Assert.That(tick.Single(value => value.RacerId == "player").ExpiredEffectCount, Is.EqualTo(1));
            Assert.That(tick.Single(value => value.RacerId == "rival-a").ExpiredEffectCount, Is.EqualTo(1));

            runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 2d);
            runtime.ResetRace();

            Assert.That(runtime.GetActiveEffect("player", PowerUpKind.NitroSpirit, 2d), Is.Null);
            var restored = runtime.GetInventorySnapshot("player", 2d)
                .Single(value => value.Kind == PowerUpKind.NitroSpirit);
            Assert.That(restored.Charges, Is.EqualTo(2));
            Assert.That(restored.CooldownRemainingSeconds, Is.Zero);
        }

        [Test]
        public void InvalidTargetAndMissingTarget_FailClosedWithoutConsumption()
        {
            var runtime = Runtime(Rules(trafficCharges: 1));

            var missing = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, null, 0d);
            var self = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "rival-a", 0d);
            var unknown = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "ghost", 0d);
            var inventory = runtime.GetInventorySnapshot("rival-a", 0d)
                .Single(value => value.Kind == PowerUpKind.TrafficCurse);

            Assert.That(missing.Status, Is.EqualTo(PowerUpRuntimeUseStatus.MissingTarget));
            Assert.That(self.Status, Is.EqualTo(PowerUpRuntimeUseStatus.InvalidTarget));
            Assert.That(unknown.Status, Is.EqualTo(PowerUpRuntimeUseStatus.UnknownTarget));
            Assert.That(inventory.Charges, Is.EqualTo(1));
        }

        private static PowerUpRaceRuntime Runtime(PowerUpRuntimeRuleset ruleset)
        {
            return new PowerUpRaceRuntime(
                ruleset,
                new[]
                {
                    new PowerUpRacerRegistration("rival-b"),
                    new PowerUpRacerRegistration("player"),
                    new PowerUpRacerRegistration("rival-a")
                });
        }

        private static PowerUpRuntimeRuleset Rules(
            int nitroCharges = 0,
            double nitroCooldown = 0d,
            int eyeShieldCharges = 0,
            int trafficCharges = 0,
            int enchantedCharges = 0,
            double enchantedCooldown = 0d,
            int asphaltCharges = 0,
            double effectDuration = 10d)
        {
            return new PowerUpRuntimeRuleset(new List<PowerUpRuntimeRule>
            {
                Rule(PowerUpKind.AsphaltShard, asphaltCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.WorldDeployable),
                Rule(PowerUpKind.NitroSpirit, nitroCharges, nitroCooldown, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Self),
                Rule(PowerUpKind.TrafficCurse, trafficCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Opponent),
                Rule(PowerUpKind.EnchantedPound, enchantedCharges, enchantedCooldown, effectDuration, PowerUpRefreshPolicy.IgnoreWhileActive, PowerUpRuntimeTargetMode.Self),
                Rule(PowerUpKind.EyeShield, eyeShieldCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Self)
            });
        }

        private static PowerUpRuntimeRule Rule(
            PowerUpKind kind,
            int charges,
            double cooldown,
            double duration,
            PowerUpRefreshPolicy refreshPolicy,
            PowerUpRuntimeTargetMode targetMode)
        {
            return new PowerUpRuntimeRule(
                kind,
                new PowerUpEffectSpec(kind, duration, 1d, refreshPolicy),
                charges,
                cooldown,
                targetMode);
        }
    }
}
