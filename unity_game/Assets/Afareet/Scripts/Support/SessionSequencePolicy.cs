namespace Afareet.Support
{
    public static class SessionSequencePolicy
    {
        public static int Next(int current)
        {
            if (current < 0) return 1;
            return current == int.MaxValue ? 1 : current + 1;
        }
    }
}
