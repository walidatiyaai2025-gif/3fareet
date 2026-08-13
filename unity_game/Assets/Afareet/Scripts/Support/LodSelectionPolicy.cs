namespace Afareet.Support
{
    public static class LodSelectionPolicy
    {
        public static int Select(float screenFraction)
        {
            if (screenFraction >= .18f) return 0;
            if (screenFraction >= .07f) return 1;
            return 2;
        }
    }
}
