using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class VehicleEngineAudioPresentation : MonoBehaviour
    {
        private const float ReferenceTopSpeedKph = 220f;
        private static AudioClip lowEngineClip;
        private static AudioClip highEngineClip;

        private ArcadeCarController car;
        private AudioSource lowSource;
        private AudioSource highSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<VehicleEngineAudioBootstrap>() != null) return;
            var host = new GameObject("3FAREET VEHICLE AUDIO BOOTSTRAP");
            DontDestroyOnLoad(host);
            host.AddComponent<VehicleEngineAudioBootstrap>();
        }

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            if (car == null)
            {
                enabled = false;
                return;
            }

            lowEngineClip ??= BuildEngineLoop("3Fareet Engine Low", 58f, .24f);
            highEngineClip ??= BuildEngineLoop("3Fareet Engine High", 92f, .38f);

            var isPlayer = gameObject.name.Contains("PLAYER HERO");
            lowSource = CreateLoopSource("Engine Low Layer", lowEngineClip, isPlayer);
            highSource = CreateLoopSource("Engine High Layer", highEngineClip, isPlayer);
            lowSource.Play();
            highSource.Play();
        }

        private void Update()
        {
            if (car == null || lowSource == null || highSource == null) return;

            var speed01 = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / ReferenceTopSpeedKph);
            var throttle01 = Mathf.Clamp01(Mathf.Abs(car.CurrentThrottleInput));

            var lowPitch = Mathf.Lerp(.72f, 1.28f, speed01);
            var highPitch = Mathf.Lerp(.90f, 1.92f, speed01);
            var lowVolume = Mathf.Clamp(Mathf.Lerp(.20f, .045f, speed01) + throttle01 * .035f, 0f, .24f);
            var highVolume = Mathf.Clamp(Mathf.SmoothStep(0f, .20f, speed01) + throttle01 * .045f, 0f, .24f);

            lowSource.pitch = Mathf.MoveTowards(lowSource.pitch, lowPitch, Time.unscaledDeltaTime * 1.8f);
            highSource.pitch = Mathf.MoveTowards(highSource.pitch, highPitch, Time.unscaledDeltaTime * 2.2f);
            lowSource.volume = Mathf.MoveTowards(lowSource.volume, lowVolume, Time.unscaledDeltaTime * .55f);
            highSource.volume = Mathf.MoveTowards(highSource.volume, highVolume, Time.unscaledDeltaTime * .65f);
        }

        private AudioSource CreateLoopSource(string sourceName, AudioClip clip, bool isPlayer)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.spatialBlend = isPlayer ? .15f : 1f;
            source.minDistance = 4f;
            source.maxDistance = 42f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.volume = 0f;
            return source;
        }

        private static AudioClip BuildEngineLoop(string clipName, float baseHz, float grit)
        {
            const int sampleRate = 22050;
            const float duration = 1f;
            var sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var fundamental = Mathf.Sin(2f * Mathf.PI * baseHz * t) * .42f;
                var second = Mathf.Sin(2f * Mathf.PI * baseHz * 2f * t) * .24f;
                var third = Mathf.Sin(2f * Mathf.PI * baseHz * 3f * t) * .12f;
                var texture = Mathf.Sin(2f * Mathf.PI * (baseHz * 5.1f) * t) * grit * .08f;
                data[i] = Mathf.Clamp((fundamental + second + third + texture) * .72f, -.85f, .85f);
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    internal sealed class VehicleEngineAudioBootstrap : MonoBehaviour
    {
        private float nextScan;

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;

            foreach (var car in FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None))
            {
                if (car.GetComponent<VehicleEngineAudioPresentation>() == null)
                    car.gameObject.AddComponent<VehicleEngineAudioPresentation>();
                if (car.GetComponent<VehicleDrivingSfxPresentation>() == null)
                    car.gameObject.AddComponent<VehicleDrivingSfxPresentation>();
            }
        }
    }
}
