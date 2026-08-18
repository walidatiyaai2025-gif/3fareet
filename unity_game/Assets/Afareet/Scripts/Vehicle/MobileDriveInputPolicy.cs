using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Keeps touch and motion-device driving predictable without changing desktop,
    /// AI, or controller input ranges. Motion helpers are deterministic/pure so the
    /// runtime HUD can smooth sensor input without embedding tuning literals.
    /// </summary>
    public static class MobileDriveInputPolicy
    {
        public const float TouchSteerMagnitude = 0.60f;
        public const float ReverseThrottle = -0.62f;
        public const float BrakeToReverseThresholdKph = 3f;

        public const float TiltDeadZone = 0.08f;
        public const float TiltSteerGain = 1.6f;
        public const float TiltSteerSmoothingPerSecond = 10f;

        public const float TiltThrottleDeadZone = 0.06f;
        public const float TiltCruiseThrottle = 0.58f;
        public const float TiltForwardBoostGain = 2.0f;
        public const float TiltBackwardCoastGain = 1.6f;
        public const float TiltThrottleSmoothingPerSecond = 6f;

        public static float ResolveTouchSteer(float direction)
        {
            return Mathf.Clamp(direction, -1f, 1f) * TouchSteerMagnitude;
        }

        public static float ResolveTiltSteer(float steeringTilt)
        {
            var normalized = ResolveSignedDeadZone(steeringTilt, TiltDeadZone);
            return Mathf.Clamp(
                normalized * TiltSteerGain,
                -TouchSteerMagnitude,
                TouchSteerMagnitude);
        }

        /// <summary>
        /// Resolves calibrated landscape phone pitch into a hands-free accelerator demand.
        /// Neutral posture cruises at a moderate throttle; forward pitch boosts toward full
        /// acceleration, while backward pitch progressively coasts toward zero. Motion input
        /// never creates negative throttle, braking, reverse, drift, or Spirit/Nitro demand.
        /// </summary>
        public static float ResolveTiltCruiseThrottle(float forwardTilt)
        {
            if (Mathf.Abs(forwardTilt) <= TiltThrottleDeadZone)
                return TiltCruiseThrottle;

            if (forwardTilt > TiltThrottleDeadZone)
            {
                var normalizedForward = Mathf.Clamp01(
                    (forwardTilt - TiltThrottleDeadZone) / (1f - TiltThrottleDeadZone));
                var boost = Mathf.Clamp01(normalizedForward * TiltForwardBoostGain);
                return Mathf.Lerp(TiltCruiseThrottle, 1f, boost);
            }

            var normalizedBackward = Mathf.Clamp01(
                (-forwardTilt - TiltThrottleDeadZone) / (1f - TiltThrottleDeadZone));
            var coast = Mathf.Clamp01(normalizedBackward * TiltBackwardCoastGain);
            return Mathf.Lerp(TiltCruiseThrottle, 0f, coast);
        }

        public static float SmoothTiltSteer(float current, float target, float deltaTime)
        {
            return SmoothToward(current, target, deltaTime, TiltSteerSmoothingPerSecond);
        }

        public static float SmoothTiltThrottle(float current, float target, float deltaTime)
        {
            return SmoothToward(current, target, deltaTime, TiltThrottleSmoothingPerSecond);
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

        private static float ResolveSignedDeadZone(float value, float deadZone)
        {
            var magnitude = Mathf.Abs(value);
            if (magnitude <= deadZone)
                return 0f;

            var normalized = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
            return Mathf.Sign(value) * normalized;
        }

        private static float SmoothToward(float current, float target, float deltaTime, float responsePerSecond)
        {
            if (deltaTime <= 0f)
                return current;

            var blend = 1f - Mathf.Exp(-responsePerSecond * deltaTime);
            return Mathf.Lerp(current, target, blend);
        }
    }
}
