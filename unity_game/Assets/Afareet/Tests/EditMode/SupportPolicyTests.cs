using NUnit.Framework;
using Afareet.Support;

namespace Afareet.Tests.Support
{
    public sealed class SupportPolicyTests
    {
        [Test]
        public void ReleaseLoggingFiltersDebug()
        {
            Assert.IsFalse(StructuredLogPolicy.ShouldEmit(LogSeverity.Debug, LogSeverity.Trace, true));
            Assert.IsTrue(StructuredLogPolicy.ShouldEmit(LogSeverity.Warning, LogSeverity.Info, true));
        }

        [Test]
        public void InputIntentIsClamped()
        {
            var value = InputIntentNormalizer.Normalize(2f, 2f, -1f, true);
            Assert.AreEqual(1f, value.Steering);
            Assert.AreEqual(1f, value.Throttle);
            Assert.AreEqual(0f, value.Brake);
            Assert.IsTrue(value.Nitro);
        }

        [Test]
        public void AiPersonalityIsSeeded()
        {
            var a = AiPersonalityPolicy.Build(42, 2);
            var b = AiPersonalityPolicy.Build(42, 2);
            Assert.AreEqual(a.Aggression, b.Aggression);
            Assert.AreEqual(a.LaneBias, b.LaneBias);
        }

        [Test]
        public void RaceUiRestartNeedsResults()
        {
            var flow = new RaceUiFlow();
            Assert.IsFalse(flow.RequestRestart());
            Assert.IsTrue(flow.ShowResults());
            Assert.IsTrue(flow.RequestRestart());
        }

        [Test]
        public void MusicPlayIsIdempotentForSameTrack()
        {
            var music = new MusicLifecycleState();
            Assert.IsTrue(music.Play("race_theme"));
            var generation = music.PlayerGeneration;
            Assert.IsFalse(music.Play("race_theme"));
            Assert.AreEqual(generation, music.PlayerGeneration);
        }
    }
}
