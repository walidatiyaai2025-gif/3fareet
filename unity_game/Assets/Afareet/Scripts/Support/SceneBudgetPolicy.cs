namespace Afareet.Support
{
    public static class SceneBudgetPolicy
    {
        public static int ItemCount(int tier)
        {
            if (tier <= 0) return 4;
            if (tier == 1) return 7;
            return 10;
        }
    }
}
