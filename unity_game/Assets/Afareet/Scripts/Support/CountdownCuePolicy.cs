namespace Afareet.Support
{
    public static class CountdownCuePolicy
    {
        public static int VisibleNumber(float secondsRemaining)
        {
            if (secondsRemaining <= 0f) return 0;
            var value = (int)secondsRemaining;
            if (secondsRemaining > value) value++;
            return value > 3 ? 3 : value;
        }

        public static bool ShowGo(float secondsRemaining) => secondsRemaining <= 0f;
    }
}
