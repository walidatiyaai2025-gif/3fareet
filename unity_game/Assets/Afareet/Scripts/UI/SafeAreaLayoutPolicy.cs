using System;

namespace Afareet.UI
{
    public readonly struct SafeAreaMargins
    {
        public SafeAreaMargins(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
    }

    public static class SafeAreaLayoutPolicy
    {
        public static SafeAreaMargins Normalize(float screenWidth, float screenHeight, float x, float y, float width, float height)
        {
            if (screenWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(screenWidth));
            if (screenHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(screenHeight));

            var left = Clamp01(x / screenWidth);
            var right = Clamp01((screenWidth - (x + width)) / screenWidth);
            var bottom = Clamp01(y / screenHeight);
            var top = Clamp01((screenHeight - (y + height)) / screenHeight);
            return new SafeAreaMargins(left, right, top, bottom);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
