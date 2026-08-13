namespace Afareet.Support
{
    public static class IndexBackoffPolicy
    {
        public static int Previous(int current)
        {
            return current <= 0 ? 0 : current - 1;
        }
    }
}
