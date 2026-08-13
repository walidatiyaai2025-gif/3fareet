using UnityEngine;
using UnityEngine.Profiling;

namespace Afareet.Core
{
    public sealed class PerformanceBaselineCapture : MonoBehaviour
    {
        private const int TargetSamples = 300;
        private int samples;
        private double cpuMs;
        private double gpuMs;
        private float frameSeconds;
        private long peakReservedBytes;
        private readonly FrameTiming[] timing = new FrameTiming[1];

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
            }

            frameSeconds += Time.unscaledDeltaTime;
            peakReservedBytes = System.Math.Max(peakReservedBytes, Profiler.GetTotalReservedMemoryLong());
            samples++;

            if (samples == TargetSamples) Report();
        }

        private void Report()
        {
            var fps = frameSeconds <= 0f ? 0f : samples / frameSeconds;
            var avgCpu = cpuMs / samples;
            var avgGpu = gpuMs / samples;
            var peakMb = peakReservedBytes / (1024f * 1024f);
            Debug.Log($"[UPER-002] samples={samples} avgFps={fps:0.0} avgCpuMs={avgCpu:0.00} avgGpuMs={avgGpu:0.00} peakReservedMb={peakMb:0.0} device={SystemInfo.deviceModel} gpu={SystemInfo.graphicsDeviceName}");
        }
    }
}
