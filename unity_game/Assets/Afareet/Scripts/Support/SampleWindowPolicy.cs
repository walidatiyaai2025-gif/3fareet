namespace Afareet.Support
{
    public static class SampleWindowPolicy
    {
        public static int Start(int index, int size)
        {
            if (size <= 0) return 0;
            if (index < size) return 0;
            return index - size + 1;
        }
    }
}
