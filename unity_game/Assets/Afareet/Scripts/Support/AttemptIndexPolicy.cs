namespace Afareet.Support
{
    public static class AttemptIndexPolicy
    {
        public static int Limit(int value, int maximum)
        {
            if (value < 0) return 0;
            if (value >= maximum) return maximum - 1;
            return value;
        }
    }
}
