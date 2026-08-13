namespace Afareet.Support
{
    public static class CameraFovResponsePolicy
    {
        public static float Target(float speed01, float boost01)
        {
            var s = speed01 < 0f ? 0f : (speed01 > 1f ? 1f : speed01);
            var b = boost01 < 0f ? 0f : (boost01 > 1f ? 1f : boost01);
            return 62f + (8f * s) + (6f * b);
        }
    }
}
