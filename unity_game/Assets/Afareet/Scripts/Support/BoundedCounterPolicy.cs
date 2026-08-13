namespace Afareet.Support
{
    public static class BoundedCounterPolicy
    {
        public static int Add(int current, int delta, int min, int max)
        {
            var next = current + delta;
            if (next < min) return min;
            if (next > max) return max;
            return next;
        }
    }
}
