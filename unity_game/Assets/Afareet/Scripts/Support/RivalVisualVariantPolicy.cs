using System;

namespace Afareet.Support
{
    public static class RivalVisualVariantPolicy
    {
        public static int VariantIndex(string racerId, int variantCount)
        {
            if (string.IsNullOrWhiteSpace(racerId)) throw new ArgumentException("Racer id is required.", nameof(racerId));
            if (variantCount < 1) throw new ArgumentOutOfRangeException(nameof(variantCount));

            unchecked
            {
                var hash = 17;
                for (var i = 0; i < racerId.Length; i++) hash = hash * 31 + racerId[i];
                return (hash & 0x7fffffff) % variantCount;
            }
        }

        public static float AccentStrength(int variantIndex)
        {
            return .55f + (Math.Abs(variantIndex) % 4) * .1f;
        }
    }
}
