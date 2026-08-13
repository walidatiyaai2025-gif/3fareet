namespace Afareet.Support
{
    public static class StepRatePolicy
    {
        public static int TargetMs(int tier)
        {
            if (tier <= 0) return 33;
            if (tier == 1) return 22;
            return 16;
        }
    }
}
