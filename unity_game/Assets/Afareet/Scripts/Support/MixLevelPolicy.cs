namespace Afareet.Support
{
    public static class MixLevelPolicy
    {
        public static float Primary(bool paused) => paused ? 0.5f : 1f;
        public static float Secondary(bool paused) => paused ? 0.4f : 1f;
    }
}
