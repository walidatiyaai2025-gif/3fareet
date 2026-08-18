using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class PowerUpPresentationHubTests
    {
        [Test]
        public void SinkPublishesRacerScopedEnvelope()
        {
            RacerPowerUpPresentationEvent captured = null;
            void Handler(RacerPowerUpPresentationEvent value) => captured = value;
            PowerUpPresentationHub.Published += Handler;
            try
            {
                var sink = PowerUpPresentationHub.CreateSink("RIVAL-01");
                var evt = new PowerUpPresentationEvent(
                    1,
                    PowerUpPresentationEventKind.Applied,
                    PowerUpKind.NitroSpirit,
                    2.5d,
                    1.2d);

                sink.Publish(evt);

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.RacerId, Is.EqualTo("RIVAL-01"));
                Assert.That(captured.Event, Is.SameAs(evt));
            }
            finally
            {
                PowerUpPresentationHub.Published -= Handler;
            }
        }

        [Test]
        public void SinkPreservesBlockedAndResetEventsWithoutPresentationLogic()
        {
            var count = 0;
            void Handler(RacerPowerUpPresentationEvent _) => count++;
            PowerUpPresentationHub.Published += Handler;
            try
            {
                var sink = PowerUpPresentationHub.CreateSink("PLAYER");
                sink.Publish(new PowerUpPresentationEvent(
                    1,
                    PowerUpPresentationEventKind.Blocked,
                    PowerUpKind.TrafficCurse,
                    1d,
                    .8d));
                sink.Publish(new PowerUpPresentationEvent(
                    2,
                    PowerUpPresentationEventKind.RaceReset,
                    null,
                    3d,
                    0d));
                Assert.That(count, Is.EqualTo(2));
            }
            finally
            {
                PowerUpPresentationHub.Published -= Handler;
            }
        }
    }
}
