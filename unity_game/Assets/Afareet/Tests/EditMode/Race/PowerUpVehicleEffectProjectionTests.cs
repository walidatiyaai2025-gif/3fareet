using System;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class PowerUpVehicleEffectProjectionTests
    {
        [Test]
        public void EmptyAndEyeShieldOnly_ProjectNeutralModifiers()
        {
            var empty = PowerUpVehicleEffectProjectionPolicy.Project(Array.Empty<ActivePowerUpEffect>());
            var shield = PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.EyeShield, 1d)
            });

            AssertNeutral(empty);
            Assert.That(shield.AccelerationMultiplier, Is.EqualTo(1d));
            Assert.That(shield.MaxSpeedMultiplier, Is.EqualTo(1d));
            Assert.That(shield.SteeringAuthorityMultiplier, Is.EqualTo(1d));
            Assert.That(shield.GripMultiplier, Is.EqualTo(1d));
            Assert.That(shield.RewardMultiplier, Is.EqualTo(1d));
            Assert.That(shield.SourceEffectCount, Is.EqualTo(1));
        }

        [Test]
        public void RetainedEffects_ProjectIntendedBoundedModifierFamilies()
        {
            var asphalt = PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.AsphaltShard, .35d)
            });
            var nitro = PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.NitroSpirit, .20d)
            });
            var traffic = PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.TrafficCurse, .25d)
            });
            var reward = PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.EnchantedPound, 1.5d)
            });

            Assert.That(asphalt.SteeringAuthorityMultiplier, Is.EqualTo(.65d).Within(.0001d));
            Assert.That(asphalt.GripMultiplier, Is.EqualTo(.65d).Within(.0001d));
            Assert.That(nitro.AccelerationMultiplier, Is.EqualTo(1.2d).Within(.0001d));
            Assert.That(nitro.MaxSpeedMultiplier, Is.EqualTo(1.2d).Within(.0001d));
            Assert.That(traffic.AccelerationMultiplier, Is.EqualTo(.75d).Within(.0001d));
            Assert.That(traffic.MaxSpeedMultiplier, Is.EqualTo(.75d).Within(.0001d));
            Assert.That(reward.RewardMultiplier, Is.EqualTo(1.5d).Within(.0001d));
        }

        [Test]
        public void NitroAndTraffic_ComposeOrderIndependently()
        {
            var nitro = Active(PowerUpKind.NitroSpirit, .20d);
            var traffic = Active(PowerUpKind.TrafficCurse, .25d);

            var first = PowerUpVehicleEffectProjectionPolicy.Project(new[] { nitro, traffic });
            var second = PowerUpVehicleEffectProjectionPolicy.Project(new[] { traffic, nitro });

            Assert.That(first.AccelerationMultiplier, Is.EqualTo(.9d).Within(.0001d));
            Assert.That(first.MaxSpeedMultiplier, Is.EqualTo(.9d).Within(.0001d));
            Assert.That(second.AccelerationMultiplier, Is.EqualTo(first.AccelerationMultiplier).Within(.0001d));
            Assert.That(second.MaxSpeedMultiplier, Is.EqualTo(first.MaxSpeedMultiplier).Within(.0001d));
        }

        [Test]
        public void DuplicateKinds_FailClosed()
        {
            Assert.Throws<ArgumentException>(() => PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.NitroSpirit, .1d),
                Active(PowerUpKind.NitroSpirit, .2d)
            }));
        }

        [Test]
        public void RuntimeProjection_UsesAuthoritativeStateAndExpiresWithIt()
        {
            var runtime = new PowerUpRaceRuntime(
                PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
                new[]
                {
                    new PowerUpRacerRegistration("player"),
                    new PowerUpRacerRegistration("rival")
                });

            runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
            runtime.TryUse("rival", PowerUpKind.TrafficCurse, "player", .1d);

            var active = runtime.GetVehicleEffectProjection("player", .1d);
            Assert.That(active.SourceEffectCount, Is.EqualTo(2));
            Assert.That(active.AccelerationMultiplier, Is.EqualTo(.9d).Within(.0001d));

            var expired = runtime.GetVehicleEffectProjection("player", 4d);
            AssertNeutral(expired);
        }

        private static ActivePowerUpEffect Active(PowerUpKind kind, double magnitude)
        {
            return new ActivePowerUpEffect(
                new PowerUpEffectSpec(kind, 10d, magnitude, PowerUpRefreshPolicy.RefreshDuration),
                0d);
        }

        private static void AssertNeutral(PowerUpVehicleEffectProjection projection)
        {
            Assert.That(projection.AccelerationMultiplier, Is.EqualTo(1d));
            Assert.That(projection.MaxSpeedMultiplier, Is.EqualTo(1d));
            Assert.That(projection.SteeringAuthorityMultiplier, Is.EqualTo(1d));
            Assert.That(projection.GripMultiplier, Is.EqualTo(1d));
            Assert.That(projection.RewardMultiplier, Is.EqualTo(1d));
            Assert.That(projection.HasDriveModifier, Is.False);
            Assert.That(projection.HasRewardModifier, Is.False);
        }
    }
}
