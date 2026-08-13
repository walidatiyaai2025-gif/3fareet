namespace Afareet.Support
{
    public static class NumericFormatPolicy
    {
        public static int Whole(float value)
        {
            if (value <= 0f) return 0;
            return (int)(value + 0.5f);
        }
    }
}
