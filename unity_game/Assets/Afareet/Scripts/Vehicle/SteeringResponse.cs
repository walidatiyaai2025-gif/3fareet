using System;

namespace Afareet.Vehicle
{
    public static class SteeringResponse
    {
        public static float Evaluate(float rawInput, float speed, float maxSpeed, float highSpeedScale)
        {
            if (maxSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxSpeed));

            var input = Clamp(rawInput, -1f, 1f);
            var speed01 = Clamp(Math.Abs(speed) / maxSpeed, 0f, 1f);
            var minScale = Clamp(highSpeedScale, 0f, 1f);
            var scale = 1f - ((1f - minScale) * speed01);
            return input * scale;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }
}
