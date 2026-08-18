using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Keeps touch and motion-device driving predictable without changing desktop,
    /// AI, or controller input ranges. Motion helpers are deterministic/pure so the
    /// runtime controller can smooth sensor input without embedding tuning literals.
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
        public const float TiltThrottleGain = 1.8f;
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
        /// Converts forward device pitch into accelerator demand only. Backward pitch
        /// deliberately returns zero: brake/reverse and Spirit/Nitro remain explicit
        /// player actions instead of being inferred from the accelerometer.
        /// </summary>
        public static float ResolveTiltThrottle(float forwardTilt)
        {
            if (forwardTilt <= TiltThrottleDeadZone)
                return 0f;

            var normalized = Mathf.Clamp01(
                (forwardTilt - TiltThrottleDeadZone) / (1f - TiltThrottleDeadZone));
            return Mathf.Clamp01(normalized * TiltThrottleGain);
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
