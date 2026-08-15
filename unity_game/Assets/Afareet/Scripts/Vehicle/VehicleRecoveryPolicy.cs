using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    /// <summary>
    /// Conservative player-only recovery rules derived from the failed physical-device
    /// UVEH-012 run: only treat the car as stuck after sustained drive intent while grounded
    /// and effectively stationary. Recovery lands on road centerline with forward/up clearance.
    /// </summary>
    public static class VehicleRecoveryPolicy
    {
        public const float StuckSpeedThresholdKph = 2.5f;
        public const float StuckDriveInputThreshold = 0.65f;
        public const float StuckSecondsBeforeAutoRecovery = 2.4f;
        public const float PostRecoveryInputLockSeconds = 0.35f;
        public const float RecoveryForwardOffsetMeters = 2.4f;
        public const float RecoveryUpOffsetMeters = 1.05f;
        public const int MaxForwardCheckpointAdvance = 4;

        public static float AdvanceStuckTimer(
            float currentSeconds,
            bool grounded,
            float signedSpeedKph,
            float throttleInput,
            bool brakeInput,
            float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            var meaningfulDriveIntent = Math.Abs(throttleInput) >= StuckDriveInputThreshold;
            var effectivelyStationary = Math.Abs(signedSpeedKph) <= StuckSpeedThresholdKph;
            if (!grounded || brakeInput || !meaningfulDriveIntent || !effectivelyStationary)
                return 0f;

            return Math.Max(0f, currentSeconds) + deltaTime;
        }

        public static bool ShouldAutoRecover(float stuckSeconds)
        {
            return stuckSeconds >= StuckSecondsBeforeAutoRecovery;
        }

        public static bool IsRecoveryCheckpointAdvanceAllowed(
            int lastCheckpointIndex,
            int candidateCheckpointIndex,
            int checkpointCount)
        {
            if (checkpointCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointCount));
            if (candidateCheckpointIndex < 0 || candidateCheckpointIndex >= checkpointCount)
                throw new ArgumentOutOfRangeException(nameof(candidateCheckpointIndex));
            if (lastCheckpointIndex < 0)
                return true;
            if (lastCheckpointIndex >= checkpointCount)
                throw new ArgumentOutOfRangeException(nameof(lastCheckpointIndex));

            var forwardDelta = (candidateCheckpointIndex - lastCheckpointIndex + checkpointCount) % checkpointCount;
            return forwardDelta <= MaxForwardCheckpointAdvance;
        }

        public static Vector3 SafeRecoveryPosition(Vector3 centerlinePosition, Quaternion trackRotation)
        {
            return centerlinePosition +
                   trackRotation * Vector3.forward * RecoveryForwardOffsetMeters +
                   Vector3.up * RecoveryUpOffsetMeters;
        }
    }
}
