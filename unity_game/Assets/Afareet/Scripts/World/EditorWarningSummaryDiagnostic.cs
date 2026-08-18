using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Editor-only warning aggregation for the local licensed Unity review loop.
    /// Captures warning first lines for a short startup window and reports the dominant
    /// patterns as normal Log entries so warning floods can be diagnosed from one screenshot.
    /// This component is compiled out of Player behavior and changes no runtime content.
    /// </summary>
    public sealed class EditorWarningSummaryDiagnostic : MonoBehaviour
    {
#if UNITY_EDITOR
        private const float CaptureSeconds = 3f;
        private const int MaxReported = 6;
        private readonly Dictionary<string, int> warningCounts = new(StringComparer.Ordinal);
        private float stopAt;
        private bool reported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<EditorWarningSummaryDiagnostic>() != null) return;
            var host = new GameObject("AFAREET EDITOR WARNING SUMMARY DIAGNOSTIC");
            DontDestroyOnLoad(host);
            host.AddComponent<EditorWarningSummaryDiagnostic>();
        }

        private void Awake()
        {
            stopAt = Time.realtimeSinceStartup + CaptureSeconds;
            Application.logMessageReceived += OnLogMessage;
        }

        private void Update()
        {
            if (reported || Time.realtimeSinceStartup < stopAt) return;
            reported = true;
            Application.logMessageReceived -= OnLogMessage;

            var ordered = warningCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(MaxReported)
                .ToArray();

            Debug.Log(
                $"AFAREET_EDITOR_WARNING_SUMMARY totalPatterns={warningCounts.Count} " +
                $"reported={ordered.Length} captureSeconds={CaptureSeconds:F1} production=false");

            for (var index = ordered.Length - 1; index >= 0; index--)
            {
                var pair = ordered[index];
                Debug.Log(
                    $"AFAREET_EDITOR_WARNING_PATTERN rank={index + 1} count={pair.Value} " +
                    $"message={pair.Key} production=false");
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Warning || string.IsNullOrWhiteSpace(condition)) return;
            var firstLineEnd = condition.IndexOf('\n');
            var firstLine = firstLineEnd >= 0 ? condition.Substring(0, firstLineEnd) : condition;
            firstLine = firstLine.Trim();
            if (firstLine.Length > 220) firstLine = firstLine.Substring(0, 220);

            warningCounts.TryGetValue(firstLine, out var count);
            warningCounts[firstLine] = count + 1;
        }
#endif
    }
}
