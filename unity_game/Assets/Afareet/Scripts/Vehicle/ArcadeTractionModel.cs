using System;

namespace Afareet.Vehicle
{
    // Virtual racing-game traction helper only.
    public static class ArcadeTractionModel
    {
        public static float LimitDrive(float requestedDrive, float lateralSlip, float slipThreshold, float strength)
        {
            var drive = Clamp(requestedDrive, -1f, 1f);
            var threshold = Math.Max(0.001f, Math.Abs(slipThreshold));
            var slip = Math.Abs(lateralSlip);
            if (slip <= threshold)
                return drive;

            var progress = Clamp((slip - threshold) / threshold, 0f, 1f);
            var reduction = Clamp(strength, 0f, 1f) * progress;
            return drive * (1f - reduction);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
