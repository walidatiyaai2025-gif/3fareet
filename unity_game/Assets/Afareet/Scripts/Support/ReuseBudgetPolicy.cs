using System;

namespace Afareet.Support
{
    public enum ReuseTier { Low, Mid, High }

    public readonly struct ReuseBudget
    {
        public ReuseBudget(int warmCount, int maxCount) { WarmCount = warmCount; MaxCount = maxCount; }
        public int WarmCount { get; }
        public int MaxCount { get; }
    }

    public static class ReuseBudgetPolicy
    {
        public static ReuseBudget For(ReuseTier tier, int activeEstimate)
        {
            if (activeEstimate < 0) throw new ArgumentOutOfRangeException(nameof(activeEstimate));
            var multiplier = tier == ReuseTier.Low ? 1.15f : tier == ReuseTier.Mid ? 1.35f : 1.6f;
            var warm = Math.Max(4, (int)Math.Ceiling(activeEstimate * .6f));
            var max = Math.Max(warm, (int)Math.Ceiling(activeEstimate * multiplier));
            return new ReuseBudget(warm, max);
        }
    }
}
