using System;

namespace Afareet.Vehicle
{
    // Virtual racing-game helper only. It does not control real vehicles.
    public static class ArcadeGameSpeedGovernor
    {
        public static float EvaluateThrottle(float requestedThrottle, float virtualSpeed, float maxVirtualSpeed, float softZoneFraction)
        {
            if (maxVirtualSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxVirtualSpeed));

            var throttle = Clamp(requestedThrottle, -1f, 1f);
            if (throttle <= 0f)
                return throttle;

            var zone = Clamp(softZoneFraction, 0.05f, 1f);
            var speed = Math.Abs(virtualSpeed);
            var taperStart = maxVirtualSpeed * (1f - zone);
            if (speed <= taperStart) return throttle;
            if (speed >= maxVirtualSpeed) return 0f;

            var available = (maxVirtualSpeed - speed) / (maxVirtualSpeed - taperStart);
            return throttle * Clamp(available, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
