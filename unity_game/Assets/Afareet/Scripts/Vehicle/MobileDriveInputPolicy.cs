using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Keeps touch-device steering and brake/reverse behavior predictable without
    /// changing desktop, AI, or controller input ranges.
    /// </summary>
    public static class MobileDriveInputPolicy
    {
        public const float TouchSteerMagnitude = 0.60f;
        public const float ReverseThrottle = -0.62f;
        public const float BrakeToReverseThresholdKph = 3f;
        public const float TiltDeadZone = 0.08f;
        public const float TiltSteerGain = 1.6f;

        public static float ResolveTouchSteer(float direction)
        {
            return Mathf.Clamp(direction, -1f, 1f) * TouchSteerMagnitude;
        }

        public static float ResolveTiltSteer(float steeringTilt)
        {
            if (Mathf.Abs(steeringTilt) <= TiltDeadZone)
                return 0f;

            return Mathf.Clamp(
                steeringTilt * TiltSteerGain,
                -TouchSteerMagnitude,
                TouchSteerMagnitude);
        }

        public static void ResolveBrakeReverse(float speedKph, out float throttle, out bool brake)
        {
            if (speedKph > BrakeToReverseThresholdKph)
            {
                throttle = 0f;
                brake = true;
                return;
            }

            throttle = ReverseThrottle;
            brake = false;
        }
    }
}
