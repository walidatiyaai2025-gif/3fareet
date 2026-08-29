using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public sealed class PowerUpVehicleEffectProjection
    {
        public double AccelerationMultiplier { get; }
        public double MaxSpeedMultiplier { get; }
        public double SteeringAuthorityMultiplier { get; }
        public double GripMultiplier { get; }
        public double RewardMultiplier { get; }
        public int SourceEffectCount { get; }

        public bool HasDriveModifier =>
            !ApproximatelyOne(AccelerationMultiplier) ||
            !ApproximatelyOne(MaxSpeedMultiplier) ||
            !ApproximatelyOne(SteeringAuthorityMultiplier) ||
            !ApproximatelyOne(GripMultiplier);

        public bool HasRewardModifier => !ApproximatelyOne(RewardMultiplier);

        public PowerUpVehicleEffectProjection(
            double accelerationMultiplier,
            double maxSpeedMultiplier,
            double steeringAuthorityMultiplier,
            double gripMultiplier,
            double rewardMultiplier,
            int sourceEffectCount)
        {
            ValidatePositiveFinite(accelerationMultiplier, nameof(accelerationMultiplier));
            ValidatePositiveFinite(maxSpeedMultiplier, nameof(maxSpeedMultiplier));
            ValidatePositiveFinite(steeringAuthorityMultiplier, nameof(steeringAuthorityMultiplier));
            ValidatePositiveFinite(gripMultiplier, nameof(gripMultiplier));
            ValidatePositiveFinite(rewardMultiplier, nameof(rewardMultiplier));
            if (sourceEffectCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceEffectCount));
            }

            AccelerationMultiplier = accelerationMultiplier;
            MaxSpeedMultiplier = maxSpeedMultiplier;
            SteeringAuthorityMultiplier = steeringAuthorityMultiplier;
            GripMultiplier = gripMultiplier;
            RewardMultiplier = rewardMultiplier;
            SourceEffectCount = sourceEffectCount;
        }

        public static PowerUpVehicleEffectProjection Neutral()
        {
            return new PowerUpVehicleEffectProjection(1d, 1d, 1d, 1d, 1d, 0);
        }

        private static void ValidatePositiveFinite(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        private static bool ApproximatelyOne(double value)
        {
            return Math.Abs(value - 1d) <= .0000001d;
        }
    }

    public static class PowerUpVehicleEffectProjectionPolicy
    {
        public const double MinimumDriveMultiplier = .25d;
        public const double MaximumBoostMultiplier = 2d;
        public const double MinimumHandlingMultiplier = .25d;
        public const double MaximumRewardMultiplier = 5d;

        public static PowerUpVehicleEffectProjection Project(
            IEnumerable<ActivePowerUpEffect> activeEffects)
        {
            if (activeEffects == null)
            {
                throw new ArgumentNullException(nameof(activeEffects));
            }

            var seenKinds = new HashSet<PowerUpKind>();
            var acceleration = 1d;
            var maxSpeed = 1d;
            var steeringAuthority = 1d;
            var grip = 1d;
            var reward = 1d;
            var sourceEffectCount = 0;

            foreach (var effect in activeEffects)
            {
                if (effect == null)
                {
                    throw new ArgumentException("Active power-up effects cannot contain null entries.", nameof(activeEffects));
                }

                var kind = effect.Spec.Kind;
                if (!seenKinds.Add(kind))
                {
                    throw new ArgumentException(
                        $"Duplicate active power-up effect for {kind}.",
                        nameof(activeEffects));
                }

                sourceEffectCount++;
                var magnitude = effect.Spec.Magnitude;
                switch (kind)
                {
                    case PowerUpKind.AsphaltShard:
                    {
                        var handlingPenalty = Clamp(
                            1d - magnitude,
                            MinimumHandlingMultiplier,
                            1d);
                        steeringAuthority *= handlingPenalty;
                        grip *= handlingPenalty;
                        break;
                    }
                    case PowerUpKind.NitroSpirit:
                    {
                        var boost = Clamp(
                            1d + magnitude,
                            1d,
                            MaximumBoostMultiplier);
                        acceleration *= boost;
                        maxSpeed *= boost;
                        break;
                    }
                    case PowerUpKind.TrafficCurse:
                    {
                        var slow = Clamp(
                            1d - magnitude,
                            MinimumDriveMultiplier,
                            1d);
                        acceleration *= slow;
                        maxSpeed *= slow;
                        break;
                    }
                    case PowerUpKind.EnchantedPound:
                        reward *= Clamp(
                            Math.Max(1d, magnitude),
                            1d,
                            MaximumRewardMultiplier);
                        break;
                    case PowerUpKind.EyeShield:
                        // Eye Shield is an immunity gate in PowerUpEffectState and intentionally
                        // does not alter drive or reward multipliers.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(activeEffects));
                }
            }

            return new PowerUpVehicleEffectProjection(
                Clamp(acceleration, MinimumDriveMultiplier, MaximumBoostMultiplier),
                Clamp(maxSpeed, MinimumDriveMultiplier, MaximumBoostMultiplier),
                Clamp(steeringAuthority, MinimumHandlingMultiplier, 1d),
                Clamp(grip, MinimumHandlingMultiplier, 1d),
                Clamp(reward, 1d, MaximumRewardMultiplier),
                sourceEffectCount);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
