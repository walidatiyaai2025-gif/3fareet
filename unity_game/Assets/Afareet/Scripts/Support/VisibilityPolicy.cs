namespace Afareet.Support
{
    public static class VisibilityPolicy
    {
        public static int Detail(float distance)
        {
            if (distance < 60f) return 0;
            if (distance < 140f) return 1;
            return 2;
        }

        public static bool Enabled(float distance, float limit)
        {
            return distance >= 0f && limit > 0f && distance <= limit;
        }
    }
}
