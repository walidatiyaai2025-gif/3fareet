namespace Afareet.Support
{
    public static class GapIntPolicy
    {
        public static bool HasGap(int current, int previous, int gap)
        {
            return gap <= 0 || current - previous >= gap;
        }
    }
}
