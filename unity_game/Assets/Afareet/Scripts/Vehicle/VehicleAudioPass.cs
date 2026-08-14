using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class VehicleAudioPass : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private static AudioClip lowEngineClip;
        private static AudioClip highEngineClip;
        private static AudioClip driftClip;
        private static AudioClip nitroClip;
        private static AudioClip impactClip;

        private ArcadeCarController car;
        private AudioSource lowEngine;
        private AudioSource highEngine;
        private AudioSource drift;
        private AudioSource oneShots;
        private bool previousNitro;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("AFAREET Vehicle Audio Installer");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<Installer>();
        }

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            EnsureSharedClips();

            lowEngine = CreateSource(lowEngineClip, true, .42f);
            highEngine = CreateSource(highEngineClip, true, 0f);
            drift = CreateSource(driftClip, true, 0f);
            oneShots = CreateSource(null, false, .72f);

            lowEngine.Play();
            highEngine.Play();
            drift.Play();
        }

        private void Update()
        {
            if (car == null) return;

            var speed01 = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 180f);
            var throttle01 = Mathf.Abs(car.CurrentThrottleInput);

            lowEngine.pitch = Mathf.Lerp(.72f, 1.45f, speed01);
            highEngine.pitch = Mathf.Lerp(.85f, 1.8f, speed01);
            lowEngine.volume = Mathf.Lerp(.42f, .18f, speed01) + throttle01 * .08f;
            highEngine.volume = Mathf.Lerp(0f, .5f, speed01) * Mathf.Lerp(.72f, 1f, throttle01);

            var driftTarget = car.IsDrifting ? Mathf.Lerp(.22f, .5f, speed01) : 0f;
            drift.volume = Mathf.MoveTowards(drift.volume, driftTarget, Time.deltaTime * 2.8f);
            drift.pitch = Mathf.Lerp(.8f, 1.25f, speed01);

            if (car.NitroActive && !previousNitro)
                oneShots.PlayOneShot(nitroClip, .8f);
            previousNitro = car.NitroActive;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (oneShots == null || collision == null) return;
            var strength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 18f);
            if (strength < .16f) return;
            oneShots.PlayOneShot(impactClip, Mathf.Lerp(.25f, .9f, strength));
        }

        private AudioSource CreateSource(AudioClip clip, bool loop, float volume)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = .68f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 45f;
            source.dopplerLevel = .25f;
            source.volume = volume;
            return source;
        }

        private static void EnsureSharedClips()
        {
            if (lowEngineClip != null) return;
            lowEngineClip = ToneLoop("AFAREET Engine Low", 58f, .48f, .2f);
            highEngineClip = ToneLoop("AFAREET Engine High", 116f, .32f, .12f);
            driftClip = NoiseLoop("AFAREET Drift", .55f, .14f);
            nitroClip = SweepOneShot("AFAREET Nitro", 82f, 310f, .34f, .28f);
            impactClip = NoiseOneShot("AFAREET Impact", .2f, .42f);
        }

        private static AudioClip ToneLoop(string clipName, float frequency, float amplitude, float harmonic)
        {
            var length = SampleRate;
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var fundamental = Mathf.Sin(Mathf.PI * 2f * frequency * t);
                var second = Mathf.Sin(Mathf.PI * 4f * frequency * t) * harmonic;
                data[i] = (fundamental + second) * amplitude;
            }
            return BuildClip(clipName, data);
        }

        private static AudioClip NoiseLoop(string clipName, float seconds, float amplitude)
        {
            var length = Mathf.Max(64, Mathf.RoundToInt(SampleRate * seconds));
            var data = new float[length];
            var state = 9137u;
            for (var i = 0; i < length; i++)
            {
                state = state * 1664525u + 1013904223u;
                var noise = ((state >> 8) & 0xffff) / 32767.5f - 1f;
                var pulse = .55f + Mathf.Sin(i * .047f) * .25f;
                data[i] = noise * pulse * amplitude;
            }
            return BuildClip(clipName, data);
        }

        private static AudioClip SweepOneShot(string clipName, float startHz, float endHz, float seconds, float amplitude)
        {
            var length = Mathf.Max(64, Mathf.RoundToInt(SampleRate * seconds));
            var data = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                var u = i / (float)(length - 1);
                var hz = Mathf.Lerp(startHz, endHz, u);
                phase += Mathf.PI * 2f * hz / SampleRate;
                var envelope = Mathf.Sin(Mathf.PI * u);
                data[i] = Mathf.Sin(phase) * envelope * amplitude;
            }
            return BuildClip(clipName, data);
        }

        private static AudioClip NoiseOneShot(string clipName, float seconds, float amplitude)
        {
            var length = Mathf.Max(64, Mathf.RoundToInt(SampleRate * seconds));
            var data = new float[length];
            var state = 4811u;
            for (var i = 0; i < length; i++)
            {
                state = state * 1103515245u + 12345u;
                var noise = ((state >> 9) & 0xffff) / 32767.5f - 1f;
                var envelope = 1f - i / (float)length;
                data[i] = noise * envelope * envelope * amplitude;
            }
            return BuildClip(clipName, data);
        }

        private static AudioClip BuildClip(string clipName, float[] data)
        {
            var clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private sealed class Installer : MonoBehaviour
        {
            private float nextScan;

            private void Update()
            {
                if (Time.unscaledTime < nextScan) return;
                nextScan = Time.unscaledTime + .5f;

                var cars = FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None);
                if (cars.Length == 0) return;
                foreach (var candidate in cars)
                    if (candidate.GetComponent<VehicleAudioPass>() == null)
                        candidate.gameObject.AddComponent<VehicleAudioPass>();

                Destroy(gameObject);
            }
        }
    }
}
