namespace Afareet.Support
{
    public static class StateLatchPolicy
    {
        public static bool Next(bool current, bool setSignal, bool clearSignal)
        {
            if (clearSignal) return false;
            if (setSignal) return true;
            return current;
        }
    }
}
