namespace Afareet.Support
{
    public static class LightCountPolicy
    {
        public static int MaxCount(int tier)
        {
            if (tier <= 0) return 2;
            if (tier == 1) return 4;
            return 6;
        }
    }
}
