namespace Afareet.Support
{
    public static class HysteresisIntPolicy
    {
        public static int Next(int value, int current, int low, int high)
        {
            if (current <= 0) return value >= high ? 1 : 0;
            return value <= low ? 0 : 1;
        }
    }
}
