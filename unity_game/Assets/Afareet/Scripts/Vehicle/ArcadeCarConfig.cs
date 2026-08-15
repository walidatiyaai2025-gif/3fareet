using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    [CreateAssetMenu(menuName = "Afareet/Vehicle/Arcade Car Config", fileName = "ArcadeCarConfig")]
    public sealed class ArcadeCarConfig : ScriptableObject
    {
        [Header("Acceleration")]
        [Min(0.1f)] public float acceleration = 34f;
        [Min(0.1f)] public float reverseAcceleration = 15f;
        [Min(1f)] public float maxSpeedMetersPerSecond = 48f;
        [Min(0f)] public float nitroForce = 25f;

        [Header("Handling")]
        [Min(1f)] public float steerStrengthDegrees = 105f;
        [Min(0.1f)] public float grip = 8f;
        [Min(0.1f)] public float driftGrip = 2.1f;
        [Min(0.01f)] public float tractionSlipThresholdMetersPerSecond = 3.5f;
        [Range(0f, 1f)] public float tractionStrength = 0.55f;
        [Range(0f, 1f)] public float driftGripMinimumSteer = 0.2f;
        [Min(0.01f)] public float driftGripFullSlipMetersPerSecond = 5f;

        [Header("Surface - Off Road")]
        [Range(0.05f, 2f)] public float offRoadGripMultiplier = 0.58f;
        [Range(0.05f, 2f)] public float offRoadAccelerationMultiplier = 0.72f;
        [Range(0.05f, 2f)] public float offRoadMaxSpeedMultiplier = 0.66f;

        [Header("Surface - Boost")]
        [Range(0.05f, 2f)] public float boostGripMultiplier = 0.95f;
        [Range(0.05f, 2f)] public float boostAccelerationMultiplier = 1.18f;
        [Range(0.05f, 2f)] public float boostMaxSpeedMultiplier = 1.10f;

        [Header("Surface - Slippery")]
        [Range(0.05f, 2f)] public float slipperyGripMultiplier = 0.42f;
        [Range(0.05f, 2f)] public float slipperyAccelerationMultiplier = 0.75f;
        [Range(0.05f, 2f)] public float slipperyMaxSpeedMultiplier = 0.82f;

        [Header("Body")]
        [Min(1f)] public float massKilograms = 1150f;
        [Min(0f)] public float linearDamping = 0.15f;
        [Min(0f)] public float angularDamping = 3.5f;
        public Vector3 centerOfMass = new(0f, -0.45f, 0.15f);

        [Header("Spirit - Nitro")]
        [Range(0.05f, 1f)] public float nitroLowSpeedForceScale = 0.7f;
        [Range(0.05f, 1f)] public float nitroFullForceSpeedRatio = 0.72f;
        [Min(0f)] public float nitroMinimumSpeedKph = 15f;
        [Range(0.01f, 1f)] public float nitroMinimumActivationEnergy = 0.12f;
        [Min(0f)] public float nitroCooldownSeconds = 0.85f;
        [Range(0.01f, 1f)] public float nitroConsumptionPerSecond = 0.22f;
        [Range(0.01f, 1f)] public float nitroRechargePerSecond = 0.06f;

        [Header("Spirit - Drift")]
        [Range(0.01f, 1f)] public float driftEnergyGainPerSecond = 0.24f;
        [Range(0f, 1f)] public float driftEnergyDecayPerSecond = 0.10f;
        [Range(0f, 1f)] public float driftEnergyMinimumSteer = 0.2f;
        [Min(0f)] public float driftEnergyMinimumSpeedKph = 30f;
        [Min(0f)] public float driftEnergyMinimumSlipMetersPerSecond = 0.8f;
        [Min(0.01f)] public float driftEnergyFullGainSlipMetersPerSecond = 6f;

        public ArcadeSurfaceResponse SurfaceResponseFor(ArcadeSurfaceKind surface)
        {
            switch (surface)
            {
                case ArcadeSurfaceKind.Asphalt:
                    return new ArcadeSurfaceResponse(1f, 1f, 1f);
                case ArcadeSurfaceKind.OffRoad:
                    return new ArcadeSurfaceResponse(offRoadGripMultiplier, offRoadAccelerationMultiplier, offRoadMaxSpeedMultiplier);
                case ArcadeSurfaceKind.Boost:
                    return new ArcadeSurfaceResponse(boostGripMultiplier, boostAccelerationMultiplier, boostMaxSpeedMultiplier);
                case ArcadeSurfaceKind.Slippery:
                    return new ArcadeSurfaceResponse(slipperyGripMultiplier, slipperyAccelerationMultiplier, slipperyMaxSpeedMultiplier);
                default:
                    throw new ArgumentOutOfRangeException(nameof(surface), surface, null);
            }
        }

        public bool IsValid(out string error)
        {
            if (acceleration <= 0f || reverseAcceleration <= 0f)
                return Fail("Acceleration values must be positive.", out error);
            if (maxSpeedMetersPerSecond <= 0f || steerStrengthDegrees <= 0f)
                return Fail("Speed and steering values must be positive.", out error);
            if (grip <= 0f || driftGrip <= 0f || driftGrip >= grip)
                return Fail("Drift grip must be positive and lower than normal grip.", out error);
            if (tractionSlipThresholdMetersPerSecond <= 0f || tractionStrength < 0f || tractionStrength > 1f)
                return Fail("Traction tuning is invalid.", out error);
            if (driftGripMinimumSteer < 0f || driftGripMinimumSteer > 1f || driftGripFullSlipMetersPerSecond <= 0f)
                return Fail("Drift grip blend tuning is invalid.", out error);
            if (!SurfaceTuningValid(offRoadGripMultiplier, offRoadAccelerationMultiplier, offRoadMaxSpeedMultiplier) ||
                !SurfaceTuningValid(boostGripMultiplier, boostAccelerationMultiplier, boostMaxSpeedMultiplier) ||
                !SurfaceTuningValid(slipperyGripMultiplier, slipperyAccelerationMultiplier, slipperyMaxSpeedMultiplier))
                return Fail("Surface response multipliers must be within (0, 2].", out error);
            if (massKilograms <= 0f)
                return Fail("Vehicle mass must be positive.", out error);
            if (nitroForce < 0f || nitroLowSpeedForceScale <= 0f || nitroLowSpeedForceScale > 1f)
                return Fail("Nitro force tuning is invalid.", out error);
            if (nitroFullForceSpeedRatio <= 0f || nitroFullForceSpeedRatio > 1f)
                return Fail("Nitro full-force speed ratio must be within (0, 1].", out error);
            if (nitroMinimumSpeedKph < 0f || nitroMinimumActivationEnergy <= 0f || nitroMinimumActivationEnergy > 1f)
                return Fail("Nitro activation thresholds are invalid.", out error);
            if (nitroCooldownSeconds < 0f || nitroConsumptionPerSecond <= 0f || nitroRechargePerSecond <= 0f)
                return Fail("Nitro cooldown/energy rates are invalid.", out error);
            if (driftEnergyGainPerSecond <= 0f || driftEnergyDecayPerSecond < 0f)
                return Fail("Drift energy gain/decay rates are invalid.", out error);
            if (driftEnergyMinimumSteer < 0f || driftEnergyMinimumSteer > 1f || driftEnergyMinimumSpeedKph < 0f)
                return Fail("Drift energy steer/speed thresholds are invalid.", out error);
            if (driftEnergyMinimumSlipMetersPerSecond < 0f || driftEnergyFullGainSlipMetersPerSecond <= driftEnergyMinimumSlipMetersPerSecond)
                return Fail("Drift energy slip thresholds are invalid.", out error);

            error = string.Empty;
            return true;
        }

        private static bool SurfaceTuningValid(float gripMultiplier, float accelerationMultiplier, float maxSpeedMultiplier)
        {
            return gripMultiplier > 0f && gripMultiplier <= 2f &&
                   accelerationMultiplier > 0f && accelerationMultiplier <= 2f &&
                   maxSpeedMultiplier > 0f && maxSpeedMultiplier <= 2f;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
