namespace Afareet.Support
{
    public static class FeatureTierPolicy
    {
        public static bool Enabled(int tier, int requiredTier)
        {
            return tier >= requiredTier;
        }
    }
}
