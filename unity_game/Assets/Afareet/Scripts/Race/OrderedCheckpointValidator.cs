using System;

namespace Afareet.Race
{
    public enum CheckpointValidationResult
    {
        Accepted,
        Duplicate,
        OutOfOrder,
        InvalidIndex
    }

    /// <summary>
    /// Deterministic ordered-checkpoint state with no Unity scene dependency.
    /// Lap and finish ownership intentionally stay outside this class (URAC-003/URAC-005).
    /// </summary>
    public sealed class OrderedCheckpointValidator
    {
        public int CheckpointCount { get; }
        public int ExpectedCheckpointIndex { get; private set; }
        public int LastAcceptedCheckpointIndex { get; private set; } = -1;
        public int AcceptedCount { get; private set; }

        public OrderedCheckpointValidator(int checkpointCount, int firstExpectedCheckpointIndex = 0)
        {
            if (checkpointCount < 2)
                throw new ArgumentOutOfRangeException(nameof(checkpointCount), checkpointCount, "A race requires at least two checkpoints.");

            CheckpointCount = checkpointCount;
            Reset(firstExpectedCheckpointIndex);
        }

        public CheckpointValidationResult TryAccept(int checkpointIndex)
        {
            if (checkpointIndex < 0 || checkpointIndex >= CheckpointCount)
                return CheckpointValidationResult.InvalidIndex;

            if (checkpointIndex == LastAcceptedCheckpointIndex)
                return CheckpointValidationResult.Duplicate;

            if (checkpointIndex != ExpectedCheckpointIndex)
                return CheckpointValidationResult.OutOfOrder;

            LastAcceptedCheckpointIndex = checkpointIndex;
            AcceptedCount++;
            ExpectedCheckpointIndex = (ExpectedCheckpointIndex + 1) % CheckpointCount;
            return CheckpointValidationResult.Accepted;
        }

        public void Reset(int firstExpectedCheckpointIndex = 0)
        {
            if (firstExpectedCheckpointIndex < 0 || firstExpectedCheckpointIndex >= CheckpointCount)
                throw new ArgumentOutOfRangeException(nameof(firstExpectedCheckpointIndex));

            ExpectedCheckpointIndex = firstExpectedCheckpointIndex;
            LastAcceptedCheckpointIndex = -1;
            AcceptedCount = 0;
        }
    }
}
