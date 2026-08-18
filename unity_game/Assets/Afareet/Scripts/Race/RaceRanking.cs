using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public readonly struct RaceProgressSnapshot
    {
        public string RacerId { get; }
        public bool IsFinished { get; }
        public int CompletedLaps { get; }
        public int AcceptedCheckpoints { get; }
        public float SegmentProgress { get; }
        public float FinishTime { get; }
        public int StableOrder { get; }

        public RaceProgressSnapshot(string racerId, bool isFinished, int completedLaps, int acceptedCheckpoints, float segmentProgress, float finishTime, int stableOrder)
        {
            if (string.IsNullOrWhiteSpace(racerId)) throw new ArgumentException("Racer id is required.", nameof(racerId));
            if (completedLaps < 0) throw new ArgumentOutOfRangeException(nameof(completedLaps));
            if (acceptedCheckpoints < 0) throw new ArgumentOutOfRangeException(nameof(acceptedCheckpoints));
            if (segmentProgress < 0f || segmentProgress > 1f || float.IsNaN(segmentProgress)) throw new ArgumentOutOfRangeException(nameof(segmentProgress));
            if (stableOrder < 0) throw new ArgumentOutOfRangeException(nameof(stableOrder));
            if (isFinished && (finishTime < 0f || float.IsNaN(finishTime) || float.IsInfinity(finishTime))) throw new ArgumentOutOfRangeException(nameof(finishTime));

            RacerId = racerId;
            IsFinished = isFinished;
            CompletedLaps = completedLaps;
            AcceptedCheckpoints = acceptedCheckpoints;
            SegmentProgress = segmentProgress;
            FinishTime = isFinished ? finishTime : -1f;
            StableOrder = stableOrder;
        }
    }

    public readonly struct RankedRaceEntry
    {
        public int Position { get; }
        public RaceProgressSnapshot Progress { get; }

        public RankedRaceEntry(int position, RaceProgressSnapshot progress)
        {
            Position = position;
            Progress = progress;
        }
    }

    public static class RaceRanking
    {
        private static readonly Comparison<RankedRaceEntry> RankedEntryComparison = CompareEntries;

        public static IReadOnlyList<RankedRaceEntry> Rank(IReadOnlyList<RaceProgressSnapshot> racers)
        {
            if (racers == null) throw new ArgumentNullException(nameof(racers));

            // Race fields are intentionally small. Validate duplicate identities against the
            // already-copied entries so ranking needs one result list instead of allocating a
            // HashSet plus an ordered snapshot list and then a second ranked result list.
            var result = new List<RankedRaceEntry>(racers.Count);
            for (var i = 0; i < racers.Count; i++)
            {
                var racer = racers[i];
                for (var prior = 0; prior < result.Count; prior++)
                {
                    if (StringComparer.Ordinal.Equals(result[prior].Progress.RacerId, racer.RacerId))
                        throw new ArgumentException("Duplicate racer id.", nameof(racers));
                }

                result.Add(new RankedRaceEntry(0, racer));
            }

            result.Sort(RankedEntryComparison);
            for (var i = 0; i < result.Count; i++)
                result[i] = new RankedRaceEntry(i + 1, result[i].Progress);
            return result;
        }

        public static int Compare(RaceProgressSnapshot left, RaceProgressSnapshot right)
        {
            if (left.IsFinished != right.IsFinished) return left.IsFinished ? -1 : 1;

            if (left.IsFinished)
            {
                var finishComparison = left.FinishTime.CompareTo(right.FinishTime);
                if (finishComparison != 0) return finishComparison;
                return left.StableOrder.CompareTo(right.StableOrder);
            }

            var lapComparison = right.CompletedLaps.CompareTo(left.CompletedLaps);
            if (lapComparison != 0) return lapComparison;

            var checkpointComparison = right.AcceptedCheckpoints.CompareTo(left.AcceptedCheckpoints);
            if (checkpointComparison != 0) return checkpointComparison;

            var segmentComparison = right.SegmentProgress.CompareTo(left.SegmentProgress);
            if (segmentComparison != 0) return segmentComparison;

            return left.StableOrder.CompareTo(right.StableOrder);
        }

        public static RaceProgressSnapshot Capture(string racerId, RacerCheckpointTracker checkpoints, OneLapRaceTracker lap, float segmentProgress, int stableOrder)
        {
            if (checkpoints == null) throw new ArgumentNullException(nameof(checkpoints));
            if (lap == null) throw new ArgumentNullException(nameof(lap));

            return new RaceProgressSnapshot(racerId, lap.IsFinished, lap.CompletedLaps, checkpoints.AcceptedCount, segmentProgress, lap.FinishTime, stableOrder);
        }

        private static int CompareEntries(RankedRaceEntry left, RankedRaceEntry right)
        {
            return Compare(left.Progress, right.Progress);
        }
    }
}