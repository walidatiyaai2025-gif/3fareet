namespace Afareet.Support
{
    public static class ToleranceIntPolicy
    {
        public static bool Within(int actual, int expected, int tolerance)
        {
            if (tolerance < 0) tolerance = 0;
            var delta = actual - expected;
            if (delta < 0) delta = -delta;
            return delta <= tolerance;
        }
    }
}
