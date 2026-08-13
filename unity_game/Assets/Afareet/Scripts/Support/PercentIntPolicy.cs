namespace Afareet.Support
{
    public static class PercentIntPolicy
    {
        public static int Clamp(int value)
        {
            if (value < 0) return 0;
            return value > 100 ? 100 : value;
        }
    }
}
