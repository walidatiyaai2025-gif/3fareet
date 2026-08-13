namespace Afareet.Support
{
    public static class TierIndexPolicy
    {
        public static int Clamp(int value)
        {
            if (value < 0) return 0;
            return value > 2 ? 2 : value;
        }
    }
}
