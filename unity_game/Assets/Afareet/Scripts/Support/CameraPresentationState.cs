using System;

namespace Afareet.Support
{
    public struct CameraPresentationFrame
    {
        public float Motion;
        public float RollDegrees;
        public float FovKick;
    }

    public sealed class CameraPresentationState
    {
        private float pulse;
        private float drift;
        private float nitro;

        public void PushPulse(float value) { pulse = Math.Max(pulse, Clamp01(value)); }
        public void SetDrift(float value) { drift = Clamp01(value); }
        public void SetNitro(bool active) { nitro = active ? 1f : 0f; }

        public CameraPresentationFrame Tick(float deltaSeconds, float accessibilityScale)
        {
            var scale = Clamp01(accessibilityScale);
            var frame = new CameraPresentationFrame
            {
                Motion = Clamp01(pulse * 0.9f + drift * 0.18f + nitro * 0.12f) * scale,
                RollDegrees = drift * 2.2f * scale,
                FovKick = nitro * 4.5f + pulse * 1.2f
            };
            pulse = Math.Max(0f, pulse - deltaSeconds * 2.4f);
            return frame;
        }

        private static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
    }
}
