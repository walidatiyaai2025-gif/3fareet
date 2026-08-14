using System;

namespace Afareet.Race
{
    public sealed class RivalMotionGuard
    {
        private float lowSpeedSeconds;
        public RivalMotionGuard(float lowSpeedKph = 4f, float delaySeconds = 2.5f)
        {
            if (lowSpeedKph < 0f || delaySeconds <= 0f) throw new ArgumentOutOfRangeException();
            LowSpeedKph = lowSpeedKph;
            DelaySeconds = delaySeconds;
        }
        public float LowSpeedKph { get; }
        public float DelaySeconds { get; }
        public float LowSpeedSeconds => lowSpeedSeconds;

        public bool Observe(float speedKph, float deltaSeconds)
        {
            if (speedKph < 0f || deltaSeconds < 0f) throw new ArgumentOutOfRangeException();
            if (speedKph > LowSpeedKph)
            {
                lowSpeedSeconds = 0f;
                return false;
            }
            lowSpeedSeconds += deltaSeconds;
            if (lowSpeedSeconds < DelaySeconds) return false;
            lowSpeedSeconds = 0f;
            return true;
        }

        public void Reset() => lowSpeedSeconds = 0f;
    }
}
