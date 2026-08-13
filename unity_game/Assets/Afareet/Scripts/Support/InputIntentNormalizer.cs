using System;

namespace Afareet.Support
{
    public struct InputIntent
    {
        public float Steering;
        public float Throttle;
        public float Brake;
        public bool Nitro;
    }

    public static class InputIntentNormalizer
    {
        public static InputIntent Normalize(float steering, float throttle, float brake, bool nitro)
        {
            return new InputIntent
            {
                Steering = Clamp(steering, -1f, 1f),
                Throttle = Clamp(throttle, 0f, 1f),
                Brake = Clamp(brake, 0f, 1f),
                Nitro = nitro
            };
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
