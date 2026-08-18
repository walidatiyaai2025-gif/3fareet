using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class AsphaltShardDeployableRuntimeTests
    {
        [Test]
        public void UseConsumesChargeWithoutImmediatelyApplyingOpponentEffect()
        {
            var runtime = Runtime();

            var result = runtime.TryUse("SOURCE", PowerUpKind.AsphaltShard, null, 0d);

            Assert.That(result.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(result.TargetRacerId, Is.Null);
            Assert.That(result.EffectResult, Is.Null);
            Assert.That(result.RemainingCharges, Is.Zero);
            Assert.That(result.CooldownRemainingSeconds, Is.EqualTo(8d));
            Assert.That(runtime.GetActiveEffect("TARGET", PowerUpKind.AsphaltShard, 0d), Is.Null);
        }

        [Test]
        public void DeployableRejectsDirectOpponentTargetWithoutConsumingInventory()
        {
            var runtime = Runtime();

            var rejected = runtime.TryUse("SOURCE", PowerUpKind.AsphaltShard, "TARGET", 0d);

            Assert.That(rejected.Status, Is.EqualTo(PowerUpRuntimeUseStatus.InvalidTarget));
            Assert.That(runtime.GetInventorySnapshot("SOURCE", 0d)[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void TrapArmsAfterDelayIgnoresSourceAndTriggersOnce()
        {
            var traps = new AsphaltShardTrapRuntime();
            var point = new AsphaltShardTrapPoint(10d, 0d, 20d);
            var trap = traps.Deploy("SOURCE", point, 2d);

            Assert.That(trap.IsArmed(2.2d), Is.False);
            Assert.That(traps.TryTrigger("SOURCE", point, 3d, out _), Is.False);
            Assert.That(traps.TryTrigger("TARGET", point, 2.2d, out _), Is.False);
            Assert.That(traps.TryTrigger("TARGET", point, 2.35d, out var triggered), Is.True);
            Assert.That(triggered.SequenceId, Is.EqualTo(trap.SequenceId));
            Assert.That(triggered.IsConsumed, Is.True);
            Assert.That(traps.TryTrigger("OTHER", point, 2.5d, out _), Is.False);
        }

        [Test]
        public void TrapExpiresWithoutTriggerAndResetRestartsSequence()
        {
            var traps = new AsphaltShardTrapRuntime();
            var point = new AsphaltShardTrapPoint(0d, 0d, 0d);
            var first = traps.Deploy("SOURCE", point, 1d);

            Assert.That(traps.Tick(1d + AsphaltShardTrapRuntime.LifetimeSeconds), Is.EqualTo(1));
            Assert.That(traps.ActiveCount, Is.Zero);

            traps.ResetRace();
            var afterReset = traps.Deploy("SOURCE", point, 0d);
            Assert.That(first.SequenceId, Is.EqualTo(1));
            Assert.That(afterReset.SequenceId, Is.EqualTo(1));
        }

        [Test]
        public void TriggeredTrapAppliesHandlingPenaltyThroughRuntimeBridge()
        {
            var runtime = Runtime();
            runtime.TryUse("SOURCE", PowerUpKind.AsphaltShard, null, 0d);

            var impact = runtime.TryApplyDeployedEffect(
                "SOURCE",
                "TARGET",
                PowerUpKind.AsphaltShard,
                1d);

            Assert.That(impact.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            var active = runtime.GetActiveEffect("TARGET", PowerUpKind.AsphaltShard, 1d);
            Assert.That(active, Is.Not.Null);
            Assert.That(active.Spec.Magnitude, Is.EqualTo(.35d));
        }

        [Test]
        public void EyeShieldBlocksTriggeredTrapWithoutSecondInventoryConsumption()
        {
            var runtime = Runtime();
            var deployed = runtime.TryUse("SOURCE", PowerUpKind.AsphaltShard, null, 0d);
            var shield = runtime.TryUse("TARGET", PowerUpKind.EyeShield, null, 0d);

            var impact = runtime.TryApplyDeployedEffect(
                "SOURCE",
                "TARGET",
                PowerUpKind.AsphaltShard,
                1d);

            Assert.That(deployed.RemainingCharges, Is.Zero);
            Assert.That(shield.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(impact.Status, Is.EqualTo(PowerUpRuntimeUseStatus.BlockedByEyeShield));
            Assert.That(impact.RemainingCharges, Is.Zero);
            Assert.That(runtime.GetActiveEffect("TARGET", PowerUpKind.AsphaltShard, 1d), Is.Null);
        }

        private static PowerUpRaceRuntime Runtime()
        {
            return new PowerUpRaceRuntime(
                PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
                new[]
                {
                    new PowerUpRacerRegistration("SOURCE"),
                    new PowerUpRacerRegistration("TARGET"),
                    new PowerUpRacerRegistration("OTHER")
                });
        }
    }
}
