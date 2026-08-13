namespace Afareet.Support
{
    public static class SequenceIndexPolicy
    {
        public static int Wrap(int index, int count)
        {
            if (count <= 0) return 0;
            var value = index % count;
            return value < 0 ? value + count : value;
        }
    }
}
