using System;

namespace Afareet.Vehicle
{
    public static class ArcadeDriftEnergy
    {
        public static float Step(float currentEnergy, bool drifting, float slipMagnitude, float deltaTime, float gainPerSecond, float decayPerSecond)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (gainPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(gainPerSecond));
            if (decayPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(decayPerSecond));

            var energy = Clamp01(currentEnergy);
            if (drifting)
                energy += Clamp01(Math.Abs(slipMagnitude)) * gainPerSecond * deltaTime;
            else
                energy -= decayPerSecond * deltaTime;
            return Clamp01(energy);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
