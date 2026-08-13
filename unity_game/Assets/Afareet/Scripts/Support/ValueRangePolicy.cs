namespace Afareet.Support
{
    public static class ValueRangePolicy
    {
        public static float Limit(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
