using System.Diagnostics;
using UnityEngine;

namespace Afareet.Core
{
    public enum AfareetLogChannel
    {
        Core,
        Vehicle,
        Race,
        UI,
        Art,
        Audio,
        Performance,
        Release
    }

    public static class AfareetLog
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(AfareetLogChannel channel, string message)
        {
            Debug.Log(Format(channel, "INFO", message));
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(AfareetLogChannel channel, string message)
        {
            Debug.LogWarning(Format(channel, "WARN", message));
        }

        public static void Error(AfareetLogChannel channel, string message)
        {
            Debug.LogError(Format(channel, "ERROR", message));
        }

        private static string Format(AfareetLogChannel channel, string level, string message)
        {
            return $"[3FAREET][{channel}][{level}] {message}";
        }
    }
}
