using System;
namespace Afareet.Support
{
    public static class FrameBudgetPolicy
    {
        public static float Average(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0f;
            double total = 0d;
            for (int i = 0; i < samples.Length; i++) total += Math.Max(0f, samples[i]);
            return (float)(total / samples.Length);
        }
        public static bool MeetsTarget(float averageFrameMs, float targetFps)
            => targetFps > 0f && averageFrameMs <= 1000f / targetFps;
    }
}
