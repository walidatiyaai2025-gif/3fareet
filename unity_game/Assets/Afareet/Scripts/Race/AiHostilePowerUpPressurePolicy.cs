using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public static class AiHostilePowerUpPressurePolicy
    {
        public static bool HasIncomingPressure(
            double ownSpeedKph,
            bool leaderAheadCanUseAsphaltShard,
            double leaderAheadDistanceMeters,
            bool chaserBehindCanUseTrafficCurse,
            double chaserBehindDistanceMeters)
        {
            ValidateNonNegativeFinite(ownSpeedKph, nameof(ownSpeedKph));
            ValidateNonNegativeFinite(leaderAheadDistanceMeters, nameof(leaderAheadDistanceMeters));
            ValidateNonNegativeFinite(chaserBehindDistanceMeters, nameof(chaserBehindDistanceMeters));

            if (leaderAheadCanUseAsphaltShard)
            {
                var gapSeconds = AiPowerUpLiveSnapshotBuilder.EstimateGapSeconds(
                    leaderAheadDistanceMeters,
                    ownSpeedKph);
                if (gapSeconds <= AiPowerUpUsagePolicy.DefensiveChaserGapSeconds)
                    return true;
            }

            if (chaserBehindCanUseTrafficCurse)
            {
                var gapSeconds = AiPowerUpLiveSnapshotBuilder.EstimateGapSeconds(
                    chaserBehindDistanceMeters,
                    ownSpeedKph);
                if (gapSeconds <= AiPowerUpUsagePolicy.TrafficCurseMaxTargetGapSeconds)
                    return true;
            }

            return false;
        }

        public static bool IsUsable(
            IReadOnlyList<PowerUpInventorySnapshot> inventory,
            PowerUpKind kind)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            for (var index = 0; index < inventory.Count; index++)
            {
                var slot = inventory[index];
                if (slot == null)
                    throw new ArgumentException("Power-up inventory cannot contain null snapshots.", nameof(inventory));
                if (slot.Kind == kind)
                    return slot.IsUsable;
            }

            return false;
        }

        private static void ValidateNonNegativeFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
