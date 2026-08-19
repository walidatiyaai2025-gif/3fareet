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

        public const float SteeringWheelDeadZoneDegrees = 2.5f;
        public const float SteeringWheelFullLockDegrees = 28f;
        public const float TiltSteerSmoothingPerSecond = 10f;

        public const float TiltCruiseThrottle = 0.58f;
        public const float TiltThrottleSmoothingPerSecond = 6f;

        public static float ResolveTouchSteer(float direction)
        {
            return Mathf.Clamp(direction, -1f, 1f) * TouchSteerMagnitude;
        }

        /// <summary>
        /// Unity's gyroscope is right-handed while Unity transforms are left-handed.
        /// Convert the device attitude before doing relative-rotation math.
        /// </summary>
        public static Quaternion GyroToUnity(Quaternion attitude)
        {
            return new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
        }

        /// <summary>
        /// Extracts only the twist around the device screen normal (Z axis) from the
        /// calibrated relative attitude. Pitch and yaw components are intentionally
        /// discarded so moving/tilting the phone forward or backward cannot steer.
        ///
        /// Positive result means a driver's right steering-wheel turn; negative means left.
        /// </summary>
        public static float ResolveSteeringWheelDeltaDegrees(
            Quaternion baselineAttitude,
            Quaternion currentAttitude)
        {
            var relative = Quaternion.Inverse(baselineAttitude) * currentAttitude;

            // Swing-twist decomposition around Vector3.forward. For a Z-axis twist only
            // relative.z and relative.w participate; X/Y rotations cannot leak into steer.
            var twistMagnitude = Mathf.Sqrt(
                relative.z * relative.z +
                relative.w * relative.w);

            if (twistMagnitude <= 0.000001f)
                return 0f;

            var twistZ = relative.z / twistMagnitude;
            var twistW = relative.w / twistMagnitude;
            var signedDeviceAngle =
                2f * Mathf.Atan2(twistZ, twistW) * Mathf.Rad2Deg;

            // Clockwise rotation as viewed by the driver is a right turn.
            return -Mathf.DeltaAngle(0f, signedDeviceAngle);
        }

        public static float ResolveSteeringWheelInput(float steeringWheelDegrees)
        {
            var magnitude = Mathf.Abs(steeringWheelDegrees);
            if (magnitude <= SteeringWheelDeadZoneDegrees)
                return 0f;

            var normalized = Mathf.InverseLerp(
                SteeringWheelDeadZoneDegrees,
                SteeringWheelFullLockDegrees,
                magnitude);

            return Mathf.Sign(steeringWheelDegrees) *
                   normalized *
                   TouchSteerMagnitude;
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


        private static float SmoothToward(float current, float target, float deltaTime, float responsePerSecond)
        {
            if (deltaTime <= 0f)
                return current;

            var blend = 1f - Mathf.Exp(-responsePerSecond * deltaTime);
            return Mathf.Lerp(current, target, blend);
        }
    }
}
