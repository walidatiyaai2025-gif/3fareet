namespace Afareet.Support
{
    public static class IntBandPolicy
    {
        public static int Select(int value, int low, int high)
        {
            if (value >= high) return 2;
            if (value >= low) return 1;
            return 0;
        }
    }
}
