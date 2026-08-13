namespace Afareet.Support
{
    public static class ScalarBlendPolicy
    {
        public static float Blend(float a, float b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return a + ((b - a) * t);
        }
    }
}
