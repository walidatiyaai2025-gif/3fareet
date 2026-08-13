namespace Afareet.Support
{
    public static class RestartTokenPolicy
    {
        public static int Next(int current)
        {
            return current == int.MaxValue ? 1 : current + 1;
        }
    }
}
