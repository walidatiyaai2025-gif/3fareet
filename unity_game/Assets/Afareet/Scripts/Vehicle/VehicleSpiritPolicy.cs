using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    public static class VehicleSpiritPolicy
    {
        public static float NitroForceScale(float speedMetersPerSecond, float maxSpeedMetersPerSecond, float lowSpeedScale, float fullForceSpeedRatio)
        {
            if (maxSpeedMetersPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxSpeedMetersPerSecond));

            var speed01 = Mathf.Clamp01(Mathf.Abs(speedMetersPerSecond) / maxSpeedMetersPerSecond);
            var minimumScale = Mathf.Clamp(lowSpeedScale, 0f, 1f);
            var fullAt = Mathf.Clamp(fullForceSpeedRatio, 0.05f, 1f);
            var t = Mathf.Clamp01(speed01 / fullAt);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(minimumScale, 1f, t);
        }

        public static float AdvanceCooldown(float currentSeconds, float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            return Mathf.Max(0f, currentSeconds - deltaTime);
        }

        public static bool CanActivateNitro(bool requested, float energy, float cooldownRemaining, float speedKph, float minimumEnergy, float minimumSpeedKph)
        {
            return requested
                && cooldownRemaining <= 0f
                && energy >= Mathf.Clamp01(minimumEnergy)
                && speedKph >= Mathf.Max(0f, minimumSpeedKph);
        }

        public static float StepNitroEnergy(float currentEnergy, bool active, bool canRecharge, float deltaTime, float consumptionPerSecond, float rechargePerSecond)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (consumptionPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(consumptionPerSecond));
            if (rechargePerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(rechargePerSecond));

            var energy = Mathf.Clamp01(currentEnergy);
            if (active)
                energy -= consumptionPerSecond * deltaTime;
            else if (canRecharge)
                energy += rechargePerSecond * deltaTime;

            return Mathf.Clamp01(energy);
        }

        public static bool CanChargeDrift(bool driftRequested, float steerInput, float speedKph, float lateralSlipMetersPerSecond, float minimumSteer, float minimumSpeedKph, float minimumSlipMetersPerSecond)
        {
            return driftRequested
                && Mathf.Abs(steerInput) >= Mathf.Clamp01(minimumSteer)
                && Mathf.Abs(speedKph) >= Mathf.Max(0f, minimumSpeedKph)
                && Mathf.Abs(lateralSlipMetersPerSecond) >= Mathf.Max(0f, minimumSlipMetersPerSecond);
        }

        public static float StepDriftEnergy(
            float currentEnergy,
            bool driftRequested,
            float steerInput,
            float speedKph,
            float lateralSlipMetersPerSecond,
            float deltaTime,
            float gainPerSecond,
            float decayPerSecond,
            float minimumSteer,
            float minimumSpeedKph,
            float minimumSlipMetersPerSecond,
            float fullGainSlipMetersPerSecond)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (gainPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(gainPerSecond));
            if (decayPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(decayPerSecond));

            var energy = Mathf.Clamp01(currentEnergy);
            var canCharge = CanChargeDrift(
                driftRequested,
                steerInput,
                speedKph,
                lateralSlipMetersPerSecond,
                minimumSteer,
                minimumSpeedKph,
                minimumSlipMetersPerSecond);

            if (!canCharge)
                return Mathf.Clamp01(energy - decayPerSecond * deltaTime);

            var minimumSlip = Mathf.Max(0f, minimumSlipMetersPerSecond);
            var fullSlip = Mathf.Max(minimumSlip + 0.01f, fullGainSlipMetersPerSecond);
            var slip01 = Mathf.InverseLerp(minimumSlip, fullSlip, Mathf.Abs(lateralSlipMetersPerSecond));
            var gainScale = Mathf.Lerp(0.25f, 1f, slip01);
            return Mathf.Clamp01(energy + gainPerSecond * gainScale * deltaTime);
        }
    }
}
