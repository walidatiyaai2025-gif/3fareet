using System;

namespace Afareet.Vehicle
{
    public readonly struct VehiclePerformanceProfile
    {
        private const double MinimumMultiplier = 0.50d;
        private const double MaximumMultiplier = 1.50d;

        public double AccelerationMultiplier { get; }
        public double MaxSpeedMultiplier { get; }
        public double SteeringAuthorityMultiplier { get; }
        public double GripMultiplier { get; }
        public double DriftAuthorityMultiplier { get; }

        public static VehiclePerformanceProfile Identity => new VehiclePerformanceProfile(1d, 1d, 1d, 1d, 1d);

        public VehiclePerformanceProfile(
            double accelerationMultiplier,
            double maxSpeedMultiplier,
            double steeringAuthorityMultiplier,
            double gripMultiplier,
            double driftAuthorityMultiplier)
        {
            AccelerationMultiplier = Validate(accelerationMultiplier, nameof(accelerationMultiplier));
            MaxSpeedMultiplier = Validate(maxSpeedMultiplier, nameof(maxSpeedMultiplier));
            SteeringAuthorityMultiplier = Validate(steeringAuthorityMultiplier, nameof(steeringAuthorityMultiplier));
            GripMultiplier = Validate(gripMultiplier, nameof(gripMultiplier));
            DriftAuthorityMultiplier = Validate(driftAuthorityMultiplier, nameof(driftAuthorityMultiplier));
        }

        public static void ValidateInitialized(VehiclePerformanceProfile profile, string parameterName)
        {
            if (profile.AccelerationMultiplier <= 0d ||
                profile.MaxSpeedMultiplier <= 0d ||
                profile.SteeringAuthorityMultiplier <= 0d ||
                profile.GripMultiplier <= 0d ||
                profile.DriftAuthorityMultiplier <= 0d)
            {
                throw new ArgumentException("Vehicle performance profile is uninitialized or invalid.", parameterName);
            }
        }

        private static double Validate(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                value < MinimumMultiplier || value > MaximumMultiplier)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Vehicle performance multiplier must be finite and within [{MinimumMultiplier:0.00}, {MaximumMultiplier:0.00}].");
            }
            return value;
        }
    }
}
