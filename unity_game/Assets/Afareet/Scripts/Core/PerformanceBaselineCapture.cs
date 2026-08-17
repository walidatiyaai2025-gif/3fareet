using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace Afareet.Core
{
    public sealed class PerformanceBaselineCapture : MonoBehaviour
    {
        public const int TargetSamples = 300;
        public const string ReportFileName = "uper006-performance-baseline.json";
        public const int ReportSchemaVersion = 1;

        [Serializable]
        private sealed class PerformanceBaselineReport
        {
            public int schemaVersion;
            public string evidenceId;
            public string capturedUtc;
            public int samples;
            public int validFrameTimingSamples;
            public float avgFps;
            public float avgFrameMs;
            public float p95FrameMs;
            public float worstFrameMs;
            public double avgCpuMs;
            public double avgGpuMs;
            public float peakReservedMb;
            public string deviceModel;
            public string deviceName;
            public string graphicsDeviceName;
            public int graphicsMemoryMb;
            public int systemMemoryMb;
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public string platform;
            public string unityVersion;
            public string appVersion;
            public string qualityLevel;
            public int targetFrameRate;
            public int screenWidth;
            public int screenHeight;
        }

        private int samples;
        private int validTimingSamples;
        private double cpuMs;
        private double gpuMs;
        private double frameSeconds;
        private long peakReservedBytes;
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private readonly float[] frameMilliseconds = new float[TargetSamples];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Debug.isDebugBuild && !Application.isEditor) return;
            var host = new GameObject("AFAREET PERFORMANCE BASELINE");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<PerformanceBaselineCapture>();
        }

        private void Update()
        {
            if (samples >= TargetSamples) return;

            FrameTimingManager.CaptureFrameTimings();
            var count = FrameTimingManager.GetLatestTimings(1, timing);
            if (count > 0)
            {
                cpuMs += timing[0].cpuFrameTime;
                gpuMs += timing[0].gpuFrameTime;
                validTimingSamples++;
            }

            var deltaSeconds = Math.Max(0d, Time.unscaledDeltaTime);
            frameSeconds += deltaSeconds;
            frameMilliseconds[samples] = (float)(deltaSeconds * 1000d);
            peakReservedBytes = Math.Max(peakReservedBytes, Profiler.GetTotalReservedMemoryLong());
            samples++;

            if (samples == TargetSamples) Report();
        }

        private void Report()
        {
            var fps = frameSeconds <= 0d ? 0f : (float)(samples / frameSeconds);
            var avgFrameMs = frameSeconds <= 0d ? 0f : (float)(frameSeconds * 1000d / samples);
            var avgCpu = validTimingSamples <= 0 ? 0d : cpuMs / validTimingSamples;
            var avgGpu = validTimingSamples <= 0 ? 0d : gpuMs / validTimingSamples;
            var peakMb = peakReservedBytes / (1024f * 1024f);
            var sortedFrameMs = new float[samples];
            Array.Copy(frameMilliseconds, sortedFrameMs, samples);
            Array.Sort(sortedFrameMs);
            var p95Index = Math.Max(0, Math.Min(samples - 1, (int)Math.Ceiling(samples * .95d) - 1));
            var p95FrameMs = samples == 0 ? 0f : sortedFrameMs[p95Index];
            var worstFrameMs = samples == 0 ? 0f : sortedFrameMs[samples - 1];

            var report = new PerformanceBaselineReport
            {
                schemaVersion = ReportSchemaVersion,
                evidenceId = "UPER-006",
                capturedUtc = DateTime.UtcNow.ToString("O"),
                samples = samples,
                validFrameTimingSamples = validTimingSamples,
                avgFps = fps,
                avgFrameMs = avgFrameMs,
                p95FrameMs = p95FrameMs,
                worstFrameMs = worstFrameMs,
                avgCpuMs = avgCpu,
                avgGpuMs = avgGpu,
                peakReservedMb = peakMb,
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                systemMemoryMb = SystemInfo.systemMemorySize,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                platform = Application.platform.ToString(),
                unityVersion = Application.unityVersion,
                appVersion = Application.version,
                qualityLevel = QualitySettings.names.Length == 0
                    ? QualitySettings.GetQualityLevel().ToString()
                    : QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)],
                targetFrameRate = Application.targetFrameRate,
                screenWidth = Screen.width,
                screenHeight = Screen.height
            };

            var reportPath = Path.Combine(Application.persistentDataPath, ReportFileName);
            try
            {
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
                Debug.Log(
                    $"[UPER-006] samples={samples} avgFps={fps:0.0} avgFrameMs={avgFrameMs:0.00} " +
                    $"p95FrameMs={p95FrameMs:0.00} worstFrameMs={worstFrameMs:0.00} " +
                    $"avgCpuMs={avgCpu:0.00} avgGpuMs={avgGpu:0.00} validTimings={validTimingSamples} " +
                    $"peakReservedMb={peakMb:0.0} device={SystemInfo.deviceModel} gpu={SystemInfo.graphicsDeviceName} " +
                    $"report={reportPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[UPER-006] PERFORMANCE_EVIDENCE_WRITE_FAILED report={reportPath} " +
                    $"error={exception.GetType().Name}:{exception.Message}");
            }
        }
    }
}
