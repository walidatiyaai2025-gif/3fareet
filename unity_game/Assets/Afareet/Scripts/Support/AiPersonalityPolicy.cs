using System;

namespace Afareet.Support
{
    public struct AiPersonality
    {
        public float Aggression;
        public float RiskTolerance;
        public float LaneBias;
    }

    public enum OvertakeDecision { Hold, Prepare, Commit }

    public static class AiPersonalityPolicy
    {
        public static AiPersonality Build(int seed, int racerIndex)
        {
            if (racerIndex < 0) throw new ArgumentOutOfRangeException(nameof(racerIndex));
            var state = unchecked((uint)(seed * 747796405) ^ ((uint)racerIndex * 2891336453u));
            return new AiPersonality
            {
                Aggression = 0.35f + Next01(ref state) * 0.55f,
                RiskTolerance = 0.25f + Next01(ref state) * 0.65f,
                LaneBias = Next01(ref state) * 2f - 1f
            };
        }

        public static OvertakeDecision Decide(AiPersonality p, float gapMeters, float relativeSpeed, float cornerSeverity, bool laneClear)
        {
            if (!laneClear || cornerSeverity > 0.75f + 0.2f * p.RiskTolerance) return OvertakeDecision.Hold;
            var opportunity = relativeSpeed * 0.35f + (8f - Math.Min(8f, gapMeters)) * 0.08f;
            var threshold = 0.6f - p.Aggression * 0.25f;
            if (opportunity >= threshold) return OvertakeDecision.Commit;
            return opportunity >= threshold * 0.55f ? OvertakeDecision.Prepare : OvertakeDecision.Hold;
        }

        private static float Next01(ref uint state)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            return (state & 0x00FFFFFFu) / 16777215f;
        }
    }
}
