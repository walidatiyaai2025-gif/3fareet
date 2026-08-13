namespace Afareet.Support
{
    public static class TickWindowPolicy
    {
        public static bool Contains(int tick, int start, int length)
        {
            if (length <= 0) return false;
            return tick >= start && tick < start + length;
        }
    }
}
