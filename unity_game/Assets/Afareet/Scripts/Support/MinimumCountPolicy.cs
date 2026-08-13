namespace Afareet.Support
{
    public static class MinimumCountPolicy
    {
        public static bool Enough(int current, int required)
        {
            if (required <= 0) return true;
            return current >= required;
        }
    }
}
