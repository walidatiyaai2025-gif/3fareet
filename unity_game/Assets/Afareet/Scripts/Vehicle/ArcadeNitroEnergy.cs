using System;

namespace Afareet.Vehicle
{
    public readonly struct ArcadeNitroState
    {
        public ArcadeNitroState(float energy, bool active)
        {
            Energy = energy;
            Active = active;
        }

        public float Energy { get; }
        public bool Active { get; }
    }

    public static class ArcadeNitroEnergy
    {
        public static ArcadeNitroState Step(float currentEnergy, bool requestedActive, float deltaTime, float consumptionPerSecond, float rechargePerSecond)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (consumptionPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(consumptionPerSecond));
            if (rechargePerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(rechargePerSecond));

            var energy = Clamp01(currentEnergy);
            var active = requestedActive && energy > 0f;
            energy += active ? -consumptionPerSecond * deltaTime : rechargePerSecond * deltaTime;
            energy = Clamp01(energy);
            if (energy <= 0f) active = false;
            return new ArcadeNitroState(energy, active);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
