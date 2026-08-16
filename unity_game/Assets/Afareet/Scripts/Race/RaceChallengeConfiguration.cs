using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public readonly struct AiDifficultyTuning
    {
        public float PaceMultiplier { get; }
        public float AggressionMultiplier { get; }

        public AiDifficultyTuning(float paceMultiplier, float aggressionMultiplier)
        {
            if (paceMultiplier < .55f || paceMultiplier > 1.25f)
                throw new ArgumentOutOfRangeException(nameof(paceMultiplier));
            if (aggressionMultiplier < .50f || aggressionMultiplier > 1.30f)
                throw new ArgumentOutOfRangeException(nameof(aggressionMultiplier));

            PaceMultiplier = paceMultiplier;
            AggressionMultiplier = aggressionMultiplier;
        }

        public static AiDifficultyTuning Standard => new AiDifficultyTuning(1f, 1f);
    }

    public readonly struct RaceChallengeConfiguration
    {
        public int ActiveRivalCount { get; }
        public AiDifficultyTuning AiDifficulty { get; }
        public bool EliminationEnabled { get; }

        public RaceChallengeConfiguration(
            int activeRivalCount,
            AiDifficultyTuning aiDifficulty,
            bool eliminationEnabled = false)
        {
            if (activeRivalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(activeRivalCount));
            if (eliminationEnabled && activeRivalCount < 1)
                throw new ArgumentException("Elimination requires at least one rival.", nameof(activeRivalCount));

            ActiveRivalCount = activeRivalCount;
            AiDifficulty = aiDifficulty;
            EliminationEnabled = eliminationEnabled;
        }

        public static RaceChallengeConfiguration Standard =>
            new RaceChallengeConfiguration(3, AiDifficultyTuning.Standard);
    }

    public static class EliminationGatePolicy
    {
        public static IReadOnlyList<int> Build(int checkpointCount, int eliminationCount)
        {
            if (checkpointCount < 2)
                throw new ArgumentOutOfRangeException(nameof(checkpointCount));
            if (eliminationCount < 1)
                throw new ArgumentOutOfRangeException(nameof(eliminationCount));

            var gates = new List<int>(eliminationCount);
            var previous = 0;
            for (var index = 1; index <= eliminationCount; index++)
            {
                var raw = (int)Math.Ceiling(checkpointCount * (double)index / (eliminationCount + 1));
                var gate = Math.Max(previous + 1, Math.Min(checkpointCount - 1, raw));
                if (gate >= checkpointCount)
                    break;
                gates.Add(gate);
                previous = gate;
            }

            if (gates.Count == 0)
                gates.Add(Math.Max(1, checkpointCount / 2));
            return gates.AsReadOnly();
        }
    }
}
