namespace Afareet.Support
{
    public static class FlagGatePolicy
    {
        public static bool Open(bool enabled, bool complete, bool paused)
        {
            return enabled && !complete && !paused;
        }
    }
}
