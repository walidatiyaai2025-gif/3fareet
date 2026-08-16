using System.Collections.Generic;
using System.Linq;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class PowerUpPresentationHookTests
    {
        [Test]
        public void StateWithoutSink_RemainsValidAndDoesNotRequirePresentationRuntime()
        {
            var state = new PowerUpEffectState();

            Assert.DoesNotThrow(() => state.Apply(
                Spec(PowerUpKind.NitroSpirit, 4d, 1d, PowerUpRefreshPolicy.RefreshDuration),
                0d));
            Assert.That(state.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyRefreshReplaceAndBlock_EmitTypedStrictlyIncreasingCues()
        {
            var sink = new RecordingSink();
            var state = new PowerUpEffectState(sink);

            Assert.That(
                state.Apply(Spec(PowerUpKind.NitroSpirit, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d),
                Is.EqualTo(PowerUpApplyResult.Applied));
            Assert.That(
                state.Apply(Spec(PowerUpKind.NitroSpirit, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 1d),
                Is.EqualTo(PowerUpApplyResult.Refreshed));
            Assert.That(
                state.Apply(Spec(PowerUpKind.NitroSpirit, 5d, 2d, PowerUpRefreshPolicy.ReplaceIfStronger), 2d),
                Is.EqualTo(PowerUpApplyResult.Replaced));
            Assert.That(
                state.Apply(Spec(PowerUpKind.EyeShield, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 3d),
                Is.EqualTo(PowerUpApplyResult.Applied));
            Assert.That(
                state.Apply(Spec(PowerUpKind.TrafficCurse, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 3.1d),
                Is.EqualTo(PowerUpApplyResult.BlockedByEyeShield));

            CollectionAssert.AreEqual(
                new[]
                {
                    PowerUpPresentationEventKind.Applied,
                    PowerUpPresentationEventKind.Refreshed,
                    PowerUpPresentationEventKind.Replaced,
                    PowerUpPresentationEventKind.Applied,
                    PowerUpPresentationEventKind.Blocked
                },
                sink.Events.Select(value => value.EventKind).ToArray());
            CollectionAssert.AreEqual(
                new long[] { 1, 2, 3, 4, 5 },
                sink.Events.Select(value => value.SequenceId).ToArray());
            Assert.That(sink.Events.Last().Kind, Is.EqualTo(PowerUpKind.TrafficCurse));
        }

        [Test]
        public void IgnoredApplications_DoNotEmitFalsePositivePresentationCues()
        {
            var sink = new RecordingSink();
            var state = new PowerUpEffectState(sink);

            state.Apply(
                Spec(PowerUpKind.EnchantedPound, 10d, 2d, PowerUpRefreshPolicy.IgnoreWhileActive),
                0d);
            var ignored = state.Apply(
                Spec(PowerUpKind.EnchantedPound, 10d, 3d, PowerUpRefreshPolicy.IgnoreWhileActive),
                1d);

            state.Apply(
                Spec(PowerUpKind.NitroSpirit, 10d, 5d, PowerUpRefreshPolicy.ReplaceIfStronger),
                2d);
            var weakReplacement = state.Apply(
                Spec(PowerUpKind.NitroSpirit, 10d, 4d, PowerUpRefreshPolicy.ReplaceIfStronger),
                3d);

            Assert.That(ignored, Is.EqualTo(PowerUpApplyResult.IgnoredWhileActive));
            Assert.That(weakReplacement, Is.EqualTo(PowerUpApplyResult.IgnoredWhileActive));
            Assert.That(sink.Events.Count, Is.EqualTo(2));
            Assert.That(sink.Events.All(value => value.EventKind == PowerUpPresentationEventKind.Applied), Is.True);
        }

        [Test]
        public void Tick_EmitsExpiredCuesInPowerUpKindOrder()
        {
            var sink = new RecordingSink();
            var state = new PowerUpEffectState(sink);

            state.Apply(Spec(PowerUpKind.EyeShield, 1d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);
            state.Apply(Spec(PowerUpKind.EnchantedPound, 1d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);
            state.Apply(Spec(PowerUpKind.NitroSpirit, 1d, 1d, PowerUpRefreshPolicy.RefreshDuration), 0d);

            var removed = state.Tick(2d);
            var expired = sink.Events
                .Where(value => value.EventKind == PowerUpPresentationEventKind.Expired)
                .ToArray();

            Assert.That(removed, Is.EqualTo(3));
            CollectionAssert.AreEqual(
                new[]
                {
                    PowerUpKind.NitroSpirit,
                    PowerUpKind.EnchantedPound,
                    PowerUpKind.EyeShield
                },
                expired.Select(value => value.Kind.Value).ToArray());
            CollectionAssert.AreEqual(
                new long[] { 4, 5, 6 },
                expired.Select(value => value.SequenceId).ToArray());
        }

        [Test]
        public void ResetRace_ClearsStateAndEmitsExactlyOneResetCue()
        {
            var sink = new RecordingSink();
            var state = new PowerUpEffectState(sink);

            state.Apply(Spec(PowerUpKind.NitroSpirit, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 4d);
            state.Apply(Spec(PowerUpKind.EnchantedPound, 5d, 1d, PowerUpRefreshPolicy.RefreshDuration), 4d);

            state.ResetRace();

            Assert.That(state.ActiveCount, Is.Zero);
            Assert.That(sink.Events.Count, Is.EqualTo(3));
            var reset = sink.Events.Last();
            Assert.That(reset.EventKind, Is.EqualTo(PowerUpPresentationEventKind.RaceReset));
            Assert.That(reset.Kind.HasValue, Is.False);
            Assert.That(reset.SequenceId, Is.EqualTo(3));
            Assert.That(reset.RaceTimeSeconds, Is.EqualTo(4d));
        }

        private static PowerUpEffectSpec Spec(
            PowerUpKind kind,
            double duration,
            double magnitude,
            PowerUpRefreshPolicy refreshPolicy)
        {
            return new PowerUpEffectSpec(kind, duration, magnitude, refreshPolicy);
        }

        private sealed class RecordingSink : IPowerUpPresentationSink
        {
            public List<PowerUpPresentationEvent> Events { get; } = new List<PowerUpPresentationEvent>();

            public void Publish(PowerUpPresentationEvent presentationEvent)
            {
                Events.Add(presentationEvent);
            }
        }
    }
}
