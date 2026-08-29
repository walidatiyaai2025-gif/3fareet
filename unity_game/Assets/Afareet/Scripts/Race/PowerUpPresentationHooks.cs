using System;

namespace Afareet.Race
{
    public enum PowerUpPresentationEventKind
    {
        Applied = 0,
        Refreshed = 1,
        Replaced = 2,
        Blocked = 3,
        Expired = 4,
        RaceReset = 5
    }

    public sealed class PowerUpPresentationEvent
    {
        public long SequenceId { get; }
        public PowerUpPresentationEventKind EventKind { get; }
        public PowerUpKind? Kind { get; }
        public double RaceTimeSeconds { get; }
        public double Magnitude { get; }

        public PowerUpPresentationEvent(
            long sequenceId,
            PowerUpPresentationEventKind eventKind,
            PowerUpKind? kind,
            double raceTimeSeconds,
            double magnitude)
        {
            if (sequenceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceId));
            }

            if (!Enum.IsDefined(typeof(PowerUpPresentationEventKind), eventKind))
            {
                throw new ArgumentOutOfRangeException(nameof(eventKind));
            }

            if (eventKind == PowerUpPresentationEventKind.RaceReset)
            {
                if (kind.HasValue)
                {
                    throw new ArgumentException("RaceReset presentation events cannot target one power-up kind.", nameof(kind));
                }
            }
            else
            {
                if (!kind.HasValue || !Enum.IsDefined(typeof(PowerUpKind), kind.Value))
                {
                    throw new ArgumentException("Non-reset presentation events require a valid power-up kind.", nameof(kind));
                }
            }

            if (!IsFinite(raceTimeSeconds) || raceTimeSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
            }

            if (!IsFinite(magnitude) || magnitude < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            }

            SequenceId = sequenceId;
            EventKind = eventKind;
            Kind = kind;
            RaceTimeSeconds = raceTimeSeconds;
            Magnitude = magnitude;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public interface IPowerUpPresentationSink
    {
        void Publish(PowerUpPresentationEvent presentationEvent);
    }

    public sealed class NullPowerUpPresentationSink : IPowerUpPresentationSink
    {
        public static NullPowerUpPresentationSink Instance { get; } = new NullPowerUpPresentationSink();

        private NullPowerUpPresentationSink()
        {
        }

        public void Publish(PowerUpPresentationEvent presentationEvent)
        {
            if (presentationEvent == null)
            {
                throw new ArgumentNullException(nameof(presentationEvent));
            }
        }
    }
}
