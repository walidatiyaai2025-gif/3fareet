using System;

namespace Afareet.Support
{
    public readonly struct HudTelemetry
    {
        public HudTelemetry(int position, float speedKph, float spirit01, float raceTimeSeconds)
        { Position = position; SpeedKph = speedKph; Spirit01 = spirit01; RaceTimeSeconds = raceTimeSeconds; }
        public int Position { get; }
        public float SpeedKph { get; }
        public float Spirit01 { get; }
        public float RaceTimeSeconds { get; }
    }

    public static class HudTelemetryPolicy
    {
        public static HudTelemetry Normalize(int position, float speedKph, float spirit01, float raceTimeSeconds)
            => new HudTelemetry(Math.Max(1, position), Math.Max(0f, speedKph), Math.Clamp(spirit01, 0f, 1f), Math.Max(0f, raceTimeSeconds));

        public static string FormatRaceTime(float seconds)
        {
            seconds = Math.Max(0f, seconds);
            var wholeMinutes = (int)(seconds / 60f);
            var remainder = seconds - wholeMinutes * 60f;
            return $"{wholeMinutes:00}:{remainder:00.000}";
        }
    }
}
