using System;

namespace Afareet.Race
{
    public static class AiPowerUpLiveSnapshotBuilder
    {
        public const double UnknownRemainingRaceSeconds = 9999d;
        public const double MinimumGapReferenceSpeedKph = 28.8d;
        public const double MinimumTargetSpeedKph = 5d;

        public static AiPowerUpRaceSnapshot Build(
            int position,
            int fieldSize,
            int acceptedCheckpoints,
            int checkpointCount,
            double segmentProgress,
            double ownSpeedKph,
            bool hasTargetAhead,
            double targetDistanceMeters,
            double targetSpeedKph,
            bool hasChaserBehind,
            double chaserDistanceMeters,
            bool incomingHostilePressure,
            double elapsedRaceSeconds)
        {
            if (checkpointCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpointCount));
            }

            if (acceptedCheckpoints < 0 || acceptedCheckpoints > checkpointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(acceptedCheckpoints));
            }

            ValidateFiniteRange(segmentProgress, 0d, 1d, nameof(segmentProgress));
            ValidateNonNegative(ownSpeedKph, nameof(ownSpeedKph));
            ValidateNonNegative(targetDistanceMeters, nameof(targetDistanceMeters));
            ValidateNonNegative(targetSpeedKph, nameof(targetSpeedKph));
            ValidateNonNegative(chaserDistanceMeters, nameof(chaserDistanceMeters));
            ValidateNonNegative(elapsedRaceSeconds, nameof(elapsedRaceSeconds));

            var normalizedProgress = Math.Min(
                1d,
                Math.Max(0d, (acceptedCheckpoints + segmentProgress) / checkpointCount));

            var gapToTargetSeconds = hasTargetAhead
                ? EstimateGapSeconds(targetDistanceMeters, ownSpeedKph)
                : 0d;
            var gapFromChaserSeconds = hasChaserBehind
                ? EstimateGapSeconds(chaserDistanceMeters, ownSpeedKph)
                : 0d;
            var speedRatio = hasTargetAhead
                ? Math.Min(2d, ownSpeedKph / Math.Max(MinimumTargetSpeedKph, targetSpeedKph))
                : 1d;
            var remainingRaceSeconds = EstimateRemainingRaceSeconds(
                elapsedRaceSeconds,
                normalizedProgress);

            return new AiPowerUpRaceSnapshot(
                position,
                fieldSize,
                normalizedProgress,
                speedRatio,
                hasTargetAhead,
                gapToTargetSeconds,
                hasChaserBehind,
                gapFromChaserSeconds,
                incomingHostilePressure,
                remainingRaceSeconds);
        }

        public static double EstimateGapSeconds(double distanceMeters, double referenceSpeedKph)
        {
            ValidateNonNegative(distanceMeters, nameof(distanceMeters));
            ValidateNonNegative(referenceSpeedKph, nameof(referenceSpeedKph));

            var speedMetersPerSecond = Math.Max(
                MinimumGapReferenceSpeedKph,
                referenceSpeedKph) / 3.6d;
            return distanceMeters / speedMetersPerSecond;
        }

        public static double EstimateRemainingRaceSeconds(
            double elapsedRaceSeconds,
            double normalizedProgress)
        {
            ValidateNonNegative(elapsedRaceSeconds, nameof(elapsedRaceSeconds));
            ValidateFiniteRange(normalizedProgress, 0d, 1d, nameof(normalizedProgress));

            if (normalizedProgress < .05d || elapsedRaceSeconds <= 0d)
            {
                return UnknownRemainingRaceSeconds;
            }

            return Math.Max(
                0d,
                elapsedRaceSeconds * (1d - normalizedProgress) / normalizedProgress);
        }

        private static void ValidateFiniteRange(
            double value,
            double min,
            double max,
            string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        private static void ValidateNonNegative(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }
    }
}
