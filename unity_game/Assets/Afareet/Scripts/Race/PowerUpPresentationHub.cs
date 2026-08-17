using System;

namespace Afareet.Race
{
    public sealed class RacerPowerUpPresentationEvent
    {
        public string RacerId { get; }
        public PowerUpPresentationEvent Event { get; }

        public RacerPowerUpPresentationEvent(string racerId, PowerUpPresentationEvent presentationEvent)
        {
            if (string.IsNullOrWhiteSpace(racerId))
                throw new ArgumentException("Power-up presentation racer id is required.", nameof(racerId));
            RacerId = racerId;
            Event = presentationEvent ?? throw new ArgumentNullException(nameof(presentationEvent));
        }
    }

    public static class PowerUpPresentationHub
    {
        private sealed class RacerSink : IPowerUpPresentationSink
        {
            private readonly string racerId;

            public RacerSink(string racerId)
            {
                if (string.IsNullOrWhiteSpace(racerId))
                    throw new ArgumentException("Power-up presentation racer id is required.", nameof(racerId));
                this.racerId = racerId;
            }

            public void Publish(PowerUpPresentationEvent presentationEvent)
            {
                if (presentationEvent == null) throw new ArgumentNullException(nameof(presentationEvent));
                PowerUpPresentationHub.Publish(racerId, presentationEvent);
            }
        }

        public static event Action<RacerPowerUpPresentationEvent> Published;

        public static IPowerUpPresentationSink CreateSink(string racerId)
        {
            return new RacerSink(racerId);
        }

        private static void Publish(string racerId, PowerUpPresentationEvent presentationEvent)
        {
            Published?.Invoke(new RacerPowerUpPresentationEvent(racerId, presentationEvent));
        }
    }
}
