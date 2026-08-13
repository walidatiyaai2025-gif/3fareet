namespace Afareet.Support
{
    public static class ControlModePolicy
    {
        public static int Normalize(int mode)
        {
            if (mode < 0) return 0;
            return mode > 3 ? 3 : mode;
        }
    }
}
