namespace Afareet.Support
{
    public static class WeightRatioPolicy
    {
        public static float Ratio(float value, float total)
        {
            if (total <= 0f || value <= 0f) return 0f;
            var ratio = value / total;
            return ratio > 1f ? 1f : ratio;
        }
    }
}
