using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    public readonly struct WheelSuspensionCoefficients
    {
        public WheelSuspensionCoefficients(float springRate, float damperRate)
        {
            SpringRate = springRate;
            DamperRate = damperRate;
        }

        public float SpringRate { get; }
        public float DamperRate { get; }
    }

    public static class WheelSuspensionMath
    {
        public static WheelSuspensionCoefficients Calculate(
            float rigidbodyMassKilograms,
            int supportedWheelCount,
            float naturalFrequencyHz,
            float dampingRatio)
        {
            if (rigidbodyMassKilograms <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rigidbodyMassKilograms));
            if (supportedWheelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(supportedWheelCount));
            if (naturalFrequencyHz <= 0f)
                throw new ArgumentOutOfRangeException(nameof(naturalFrequencyHz));
            if (dampingRatio <= 0f || dampingRatio > 2f)
                throw new ArgumentOutOfRangeException(nameof(dampingRatio));

            var sprungMassPerWheel = rigidbodyMassKilograms / supportedWheelCount;
            var angularFrequency = 2f * Mathf.PI * naturalFrequencyHz;
            var springRate = sprungMassPerWheel * angularFrequency * angularFrequency;
            var damperRate = 2f * dampingRatio * sprungMassPerWheel * angularFrequency;
            return new WheelSuspensionCoefficients(springRate, damperRate);
        }
    }
}
