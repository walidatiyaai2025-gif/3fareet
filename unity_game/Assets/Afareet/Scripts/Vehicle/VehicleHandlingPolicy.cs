using System;

namespace Afareet.Vehicle
{
    public enum ArcadeSurfaceKind
    {
        Asphalt,
        OffRoad,
        Boost,
        Slippery
    }

    public readonly struct ArcadeSurfaceResponse
    {
        public ArcadeSurfaceResponse(float gripMultiplier, float accelerationMultiplier, float maxSpeedMultiplier)
        {
            GripMultiplier = gripMultiplier;
            AccelerationMultiplier = accelerationMultiplier;
            MaxSpeedMultiplier = maxSpeedMultiplier;
        }

        public float GripMultiplier { get; }
        public float AccelerationMultiplier { get; }
        public float MaxSpeedMultiplier { get; }
    }

    public static class VehicleHandlingPolicy
    {
        public static float LimitDriveForTraction(
            float requestedDrive,
            float lateralSlipMetersPerSecond,
            float slipThresholdMetersPerSecond,
            float tractionStrength)
        {
            if (slipThresholdMetersPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(slipThresholdMetersPerSecond));

            var drive = Clamp(requestedDrive, -1f, 1f);
            var slip = Math.Abs(lateralSlipMetersPerSecond);
            if (slip <= slipThresholdMetersPerSecond)
                return drive;

            var progress = Clamp(
                (slip - slipThresholdMetersPerSecond) / slipThresholdMetersPerSecond,
                0f,
                1f);
            var reduction = Clamp(tractionStrength, 0f, 1f) * progress;
            return drive * (1f - reduction);
        }

        public static float DriftBlend(
            bool driftRequested,
            float steerInput,
            float lateralSlipMetersPerSecond,
            float minimumSteer,
            float fullBlendSlipMetersPerSecond)
        {
            if (!driftRequested)
                return 0f;
            if (minimumSteer < 0f || minimumSteer > 1f)
                throw new ArgumentOutOfRangeException(nameof(minimumSteer));
            if (fullBlendSlipMetersPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fullBlendSlipMetersPerSecond));

            var steerMagnitude = Math.Abs(steerInput);
            if (steerMagnitude < minimumSteer)
                return 0f;

            var steerRange = Math.Max(0.001f, 1f - minimumSteer);
            var steerBlend = Clamp((steerMagnitude - minimumSteer) / steerRange, 0f, 1f);
            var slipBlend = Clamp(Math.Abs(lateralSlipMetersPerSecond) / fullBlendSlipMetersPerSecond, 0f, 1f);
            return steerBlend * slipBlend;
        }

        public static float EffectiveGrip(
            float normalGrip,
            float driftGrip,
            float driftBlend,
            float surfaceGripMultiplier)
        {
            if (normalGrip <= 0f)
                throw new ArgumentOutOfRangeException(nameof(normalGrip));
            if (driftGrip <= 0f || driftGrip >= normalGrip)
                throw new ArgumentOutOfRangeException(nameof(driftGrip));
            if (surfaceGripMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(surfaceGripMultiplier));

            var blend = Clamp(driftBlend, 0f, 1f);
            var baseGrip = normalGrip + ((driftGrip - normalGrip) * blend);
            return baseGrip * surfaceGripMultiplier;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
