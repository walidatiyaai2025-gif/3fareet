using System;
using System.Linq;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class PowerUpEffectStateTests
    {
        [Test]
        public void Apply_CreatesSingleActiveEffectPerKind()
        {
            var state = new PowerUpEffectState();
            var nitro = Spec(PowerUpKind.NitroSpirit, 3d, 1.2d, PowerUpRefreshPolicy.RefreshDuration);

            Assert.That(state.Apply(nitro, 10d), Is.EqualTo(PowerUpApplyResult.Applied));
            Assert.That(state.ActiveCount, Is.EqualTo(1));
            Assert.That(state.IsActive(PowerUpKind.NitroSpirit, 12d), Is.True);
        }

        [Test]
        public void RefreshDuration_RenewsFromApplicationTimeWithoutDuplication()
        {
            var state = new PowerUpEffectState();
            var nitro = Spec(PowerUpKind.NitroSpirit, 3d, 1.2d, PowerUpRefreshPolicy.RefreshDuration);

            state.Apply(nitro, 1d);
            Assert.That(state.Apply(nitro, 2.5d), Is.EqualTo(PowerUpApplyResult.Refreshed));

            var active = state.GetActive(PowerUpKind.NitroSpirit, 2.5d);
            Assert.That(active.ExpiresAtSeconds, Is.EqualTo(5.5d));
            Assert.That(state.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void IgnoreWhileActive_DoesNotChangeExistingEffect()
        {
            var state = new PowerUpEffectState();
            var first = Spec(PowerUpKind.EnchantedPound, 8d, 2d, PowerUpRefreshPolicy.IgnoreWhileActive);
            var second = Spec(PowerUpKind.EnchantedPound, 20d, 3d, PowerUpRefreshPolicy.IgnoreWhileActive);

            state.Apply(first, 4d);
            Assert.That(state.Apply(second, 5d), Is.EqualTo(PowerUpApplyResult.IgnoredWhileActive));

            var active = state.GetActive(PowerUpKind.EnchantedPound, 5d);
            Assert.That(active.ExpiresAtSeconds, Is.EqualTo(12d));
            Assert.That(active.Spec.Magnitude, Is.EqualTo(2d));
        }

        [Test]
        public void ReplaceIfStronger_ReplacesOnlyStrictlyStrongerMagnitude()
        {
            var state = new PowerUpEffectState();
            var medium = Spec(PowerUpKind.AsphaltShard, 4d, 0.3d, PowerUpRefreshPolicy.ReplaceIfStronger);
            var weak = Spec(PowerUpKind.AsphaltShard, 10d, 0.2d, PowerUpRefreshPolicy.ReplaceIfStronger);
            var strong = Spec(PowerUpKind.AsphaltShard, 6d, 0.5d, PowerUpRefreshPolicy.ReplaceIfStronger);

            state.Apply(medium, 0d);
            Assert.That(state.Apply(weak, 1d), Is.EqualTo(PowerUpApplyResult.IgnoredWhileActive));
            Assert.That(state.Apply(strong, 2d), Is.EqualTo(PowerUpApplyResult.Replaced));

            var active = state.GetActive(PowerUpKind.AsphaltShard, 2d);
            Assert.That(active.Spec.Magnitude, Is.EqualTo(0.5d));
            Assert.That(active.ExpiresAtSeconds, Is.EqualTo(8d));
        }

        [Test]
        public void EyeShield_BlocksHostileEffectsButAllowsBeneficialEffects()
        {
            var state = new PowerUpEffectState();
            var shield = Spec(PowerUpKind.EyeShield, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration);
            var shard = Spec(PowerUpKind.AsphaltShard, 4d, 0.4d, PowerUpRefreshPolicy.RefreshDuration);
            var curse = Spec(PowerUpKind.TrafficCurse, 4d, 0.4d, PowerUpRefreshPolicy.RefreshDuration);
            var nitro = Spec(PowerUpKind.NitroSpirit, 3d, 1.2d, PowerUpRefreshPolicy.RefreshDuration);

            state.Apply(shield, 10d);

            Assert.That(state.Apply(shard, 11d), Is.EqualTo(PowerUpApplyResult.BlockedByEyeShield));
            Assert.That(state.Apply(curse, 11d), Is.EqualTo(PowerUpApplyResult.BlockedByEyeShield));
            Assert.That(state.Apply(nitro, 11d), Is.EqualTo(PowerUpApplyResult.Applied));
            Assert.That(state.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void ExpirationAndTick_AreDeterministicAtBoundary()
        {
            var state = new PowerUpEffectState();
            state.Apply(Spec(PowerUpKind.NitroSpirit, 2d, 1d, PowerUpRefreshPolicy.RefreshDuration), 3d);
            state.Apply(Spec(PowerUpKind.EnchantedPound, 4d, 2d, PowerUpRefreshPolicy.RefreshDuration), 3d);

            Assert.That(state.Tick(4.999d), Is.EqualTo(0));
            Assert.That(state.Tick(5d), Is.EqualTo(1));
            Assert.That(state.IsActive(PowerUpKind.NitroSpirit, 5d), Is.False);
            Assert.That(state.IsActive(PowerUpKind.EnchantedPound, 5d), Is.True);
        }

        [Test]
        public void Snapshot_IsSortedByKindAndResetRaceClearsEverything()
        {
            var state = new PowerUpEffectState();
            state.Apply(Spec(PowerUpKind.EyeShield, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);
            state.Apply(Spec(PowerUpKind.NitroSpirit, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);
            state.Apply(Spec(PowerUpKind.EnchantedPound, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);

            var kinds = state.Snapshot(1d).Select(effect => effect.Spec.Kind).ToArray();
            CollectionAssert.AreEqual(
                new[] { PowerUpKind.NitroSpirit, PowerUpKind.EnchantedPound, PowerUpKind.EyeShield },
                kinds);

            state.ResetRace();
            Assert.That(state.ActiveCount, Is.EqualTo(0));
            Assert.That(state.Snapshot(1d), Is.Empty);
        }

        [Test]
        public void Spec_RejectsInvalidDurationMagnitudeAndEnumValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerUpEffectSpec(PowerUpKind.NitroSpirit, 0d, 1d, PowerUpRefreshPolicy.RefreshDuration));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerUpEffectSpec(PowerUpKind.NitroSpirit, 2d, double.NaN, PowerUpRefreshPolicy.RefreshDuration));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerUpEffectSpec((PowerUpKind)999, 2d, 1d, PowerUpRefreshPolicy.RefreshDuration));
        }

        private static PowerUpEffectSpec Spec(
            PowerUpKind kind,
            double durationSeconds,
            double magnitude,
            PowerUpRefreshPolicy refreshPolicy)
        {
            return new PowerUpEffectSpec(kind, durationSeconds, magnitude, refreshPolicy);
        }
    }
}
