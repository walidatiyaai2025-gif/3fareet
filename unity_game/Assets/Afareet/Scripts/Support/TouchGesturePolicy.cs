using System;

namespace Afareet.Support
{
    public readonly struct TouchDriveIntent
    {
        public TouchDriveIntent(float steering, float throttle, bool brake, bool nitro)
        {
            Steering = steering;
            Throttle = throttle;
            Brake = brake;
            Nitro = nitro;
        }

        public float Steering { get; }
        public float Throttle { get; }
        public bool Brake { get; }
        public bool Nitro { get; }
    }

    public static class TouchGesturePolicy
    {
        public static TouchDriveIntent Compose(float steeringAxis, float throttleAxis, bool brakePressed, bool nitroPressed, float deadZone = 0.08f)
        {
            if (deadZone < 0f || deadZone >= 1f) throw new ArgumentOutOfRangeException(nameof(deadZone));

            var steering = ClampSigned(ApplyDeadZone(steeringAxis, deadZone));
            var throttle = Clamp01(ApplyDeadZone(throttleAxis, deadZone));
            return new TouchDriveIntent(steering, throttle, brakePressed, nitroPressed && !brakePressed);
        }

        private static float ApplyDeadZone(float value, float deadZone)
        {
            if (Math.Abs(value) <= deadZone) return 0f;
            var magnitude = (Math.Abs(value) - deadZone) / (1f - deadZone);
            return Math.Sign(value) * magnitude;
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
        private static float ClampSigned(float value) => Math.Max(-1f, Math.Min(1f, value));
    }
}
