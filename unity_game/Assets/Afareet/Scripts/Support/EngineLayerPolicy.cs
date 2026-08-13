namespace Afareet.Support
{
    public static class EngineLayerPolicy
    {
        public static float Idle(float speed) => Clamp(1f - Clamp(speed) * 2f);
        public static float Mid(float speed) => Clamp(1f - System.Math.Abs(Clamp(speed) - .5f) * 2f);
        public static float High(float speed) => Clamp((Clamp(speed) - .45f) / .55f);
        private static float Clamp(float value) => System.Math.Max(0f, System.Math.Min(1f, value));
    }
}
