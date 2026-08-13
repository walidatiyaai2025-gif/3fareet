using System.Collections.Generic;
using UnityEngine;

namespace Afareet.UI
{
    public enum AfareetLocale { English, Arabic }

    public static class RuntimeLocalization
    {
        private static readonly Dictionary<string, string> En = new()
        {
            ["start"] = "START RACE",
            ["drift"] = "DRIFT",
            ["spirit"] = "SPIRIT",
            ["track"] = "CAIRO NIGHT // SPIRIT CIRCUIT"
        };

        private static readonly Dictionary<string, string> Ar = new()
        {
            ["start"] = "\u0627\u0628\u062f\u0623 \u0627\u0644\u0633\u0628\u0627\u0642",
            ["drift"] = "\u062f\u0631\u064a\u0641\u062a",
            ["spirit"] = "\u0627\u0644\u0639\u0641\u0631\u064a\u062a",
            ["track"] = "\u0627\u0644\u0642\u0627\u0647\u0631\u0629 \u0644\u064a\u0644\u0627"
        };

        public static AfareetLocale Locale { get; private set; } = Detect();
        public static bool IsRtl => Locale == AfareetLocale.Arabic;

        public static void SetLocale(AfareetLocale locale) => Locale = locale;

        public static string Text(string key)
        {
            var table = IsRtl ? Ar : En;
            return table.TryGetValue(key, out var value) ? value : key;
        }

        private static AfareetLocale Detect() =>
            Application.systemLanguage == SystemLanguage.Arabic ? AfareetLocale.Arabic : AfareetLocale.English;
    }
}
