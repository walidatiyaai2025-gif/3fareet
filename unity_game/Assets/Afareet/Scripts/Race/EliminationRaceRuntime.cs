using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public sealed class EliminationDecision
    {
        public int GateCheckpointIndex { get; }
        public string EliminatedRacerId { get; }
        public int FieldSizeBeforeElimination { get; }
        public int RemainingRacerCount => FieldSizeBeforeElimination - 1;

        internal EliminationDecision(
            int gateCheckpointIndex,
            string eliminatedRacerId,
            int fieldSizeBeforeElimination)
        {
            if (gateCheckpointIndex < 0) throw new ArgumentOutOfRangeException(nameof(gateCheckpointIndex));
            if (string.IsNullOrWhiteSpace(eliminatedRacerId))
                throw new ArgumentException("Eliminated racer id is required.", nameof(eliminatedRacerId));
            if (fieldSizeBeforeElimination < 2)
                throw new ArgumentOutOfRangeException(nameof(fieldSizeBeforeElimination));

            GateCheckpointIndex = gateCheckpointIndex;
            EliminatedRacerId = eliminatedRacerId;
            FieldSizeBeforeElimination = fieldSizeBeforeElimination;
        }
    }

    public sealed class EliminationRaceRuntime
    {
        private readonly int checkpointCount;
        private readonly IReadOnlyList<int> gates;
        private readonly HashSet<int> gateSet;
        private readonly HashSet<int> processedGates = new HashSet<int>();
        private readonly HashSet<string> eliminatedRacerIds = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<int> Gates => gates;
        public int ProcessedGateCount => processedGates.Count;
        public int EliminatedRacerCount => eliminatedRacerIds.Count;

        public EliminationRaceRuntime(int checkpointCount, int eliminationCount)
        {
            if (checkpointCount < 2) throw new ArgumentOutOfRangeException(nameof(checkpointCount));
            if (eliminationCount < 1) throw new ArgumentOutOfRangeException(nameof(eliminationCount));

            this.checkpointCount = checkpointCount;
            gates = EliminationGatePolicy.Build(checkpointCount, eliminationCount);
            gateSet = new HashSet<int>(gates);
        }

        public bool IsEliminated(string racerId)
        {
            if (string.IsNullOrWhiteSpace(racerId))
                return false;
            return eliminatedRacerIds.Contains(racerId);
        }

        public bool TryResolveGate(
            int checkpointIndex,
            IReadOnlyList<string> rankedActiveRacerIds,
            out EliminationDecision decision)
        {
            if (checkpointIndex < 0 || checkpointIndex >= checkpointCount)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));
            if (rankedActiveRacerIds == null)
                throw new ArgumentNullException(nameof(rankedActiveRacerIds));

            decision = null;
            if (!gateSet.Contains(checkpointIndex) || processedGates.Contains(checkpointIndex))
                return false;

            var active = new List<string>(rankedActiveRacerIds.Count);
            var observed = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < rankedActiveRacerIds.Count; index++)
            {
                var racerId = rankedActiveRacerIds[index];
                if (string.IsNullOrWhiteSpace(racerId))
                    throw new ArgumentException("Elimination ranking contains a blank racer id.", nameof(rankedActiveRacerIds));
                if (!observed.Add(racerId))
                    throw new ArgumentException($"Elimination ranking contains duplicate racer id '{racerId}'.", nameof(rankedActiveRacerIds));
                if (!eliminatedRacerIds.Contains(racerId))
                    active.Add(racerId);
            }

            // Consume a valid gate exactly once even if the field has already collapsed to one racer.
            // This makes duplicate checkpoint callbacks deterministic and prevents a stale gate from
            // eliminating somebody later after the ranking changes.
            processedGates.Add(checkpointIndex);
            if (active.Count < 2)
                return false;

            var eliminated = active[active.Count - 1];
            eliminatedRacerIds.Add(eliminated);
            decision = new EliminationDecision(checkpointIndex, eliminated, active.Count);
            return true;
        }

        public void Reset()
        {
            processedGates.Clear();
            eliminatedRacerIds.Clear();
        }
    }
}