using System;
using Afareet.GarageRuntime;
using Afareet.Vehicle;

namespace Afareet.Core
{
    public static class GarageVehiclePerformanceProjection
    {
        private const double MinimumStatMultiplier = 0.90d;
        private const double MaximumStatMultiplier = 1.10d;

        public static VehiclePerformanceProfile Project(GarageNormalizedStats stats)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            return new VehiclePerformanceProfile(
                Scale(stats.Acceleration),
                Scale(stats.TopSpeed),
                Scale(stats.Handling),
                Scale(stats.Handling),
                Scale(stats.Drift));
        }

        public static double Scale(float normalized)
        {
            if (float.IsNaN(normalized) || float.IsInfinity(normalized) || normalized < 0f || normalized > 1f)
                throw new ArgumentOutOfRangeException(nameof(normalized));
            return MinimumStatMultiplier + (MaximumStatMultiplier - MinimumStatMultiplier) * normalized;
        }
    }
}
