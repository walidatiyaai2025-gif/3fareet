namespace Afareet.Support
{
    public static class LocaleIndexPolicy
    {
        public static int Select(int requested, int supportedCount)
        {
            if (supportedCount <= 0) return 0;
            if (requested < 0 || requested >= supportedCount) return 0;
            return requested;
        }
    }
}
