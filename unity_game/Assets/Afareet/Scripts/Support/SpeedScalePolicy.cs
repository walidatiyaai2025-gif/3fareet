namespace Afareet.Support
{
    public static class SpeedScalePolicy
    {
        public static float Display(float metersPerSecond, bool metric)
        {
            if (metersPerSecond <= 0f) return 0f;
            return metersPerSecond * (metric ? 3.6f : 2.2369363f);
        }
    }
}
