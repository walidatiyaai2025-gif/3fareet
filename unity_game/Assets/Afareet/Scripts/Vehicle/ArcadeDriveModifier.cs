using System;

namespace Afareet.Vehicle
{
    public sealed class ArcadeDriveModifier
    {
        public const double MinimumMultiplier = .25d;
        public const double MaximumMultiplier = 2d;

        public double AccelerationMultiplier { get; }
        public double MaxSpeedMultiplier { get; }
        public double SteeringAuthorityMultiplier { get; }
        public double GripMultiplier { get; }

        public bool IsNeutral =>
            ApproximatelyOne(AccelerationMultiplier) &&
            ApproximatelyOne(MaxSpeedMultiplier) &&
            ApproximatelyOne(SteeringAuthorityMultiplier) &&
            ApproximatelyOne(GripMultiplier);

        public ArcadeDriveModifier(
            double accelerationMultiplier,
            double maxSpeedMultiplier,
            double steeringAuthorityMultiplier,
            double gripMultiplier)
        {
            ValidateMultiplier(accelerationMultiplier, nameof(accelerationMultiplier));
            ValidateMultiplier(maxSpeedMultiplier, nameof(maxSpeedMultiplier));
            ValidateMultiplier(steeringAuthorityMultiplier, nameof(steeringAuthorityMultiplier));
            ValidateMultiplier(gripMultiplier, nameof(gripMultiplier));

            AccelerationMultiplier = accelerationMultiplier;
            MaxSpeedMultiplier = maxSpeedMultiplier;
            SteeringAuthorityMultiplier = steeringAuthorityMultiplier;
            GripMultiplier = gripMultiplier;
        }

        public static ArcadeDriveModifier Neutral()
        {
            return new ArcadeDriveModifier(1d, 1d, 1d, 1d);
        }

        private static void ValidateMultiplier(double value, string paramName)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value < MinimumMultiplier ||
                value > MaximumMultiplier)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    $"Drive multiplier must be finite and within [{MinimumMultiplier}, {MaximumMultiplier}].");
            }
        }

        private static bool ApproximatelyOne(double value)
        {
            return Math.Abs(value - 1d) <= .0000001d;
        }
    }
}
