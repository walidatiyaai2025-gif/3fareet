namespace Afareet.Support
{
    public static class CycleSelectorPolicy
    {
        public static int Next(int current, int count)
        {
            if (count <= 0) return 0;
            var next = current + 1;
            return next >= count ? 0 : next;
        }
    }
}
