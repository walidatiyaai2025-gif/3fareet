using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class VehicleDrivingSfxPresentation : MonoBehaviour
    {
        private static AudioClip driftClip;
        private static AudioClip nitroLoopClip;
        private static AudioClip nitroBurstClip;
        private static AudioClip impactClip;

        private ArcadeCarController car;
        private AudioSource driftSource;
        private AudioSource nitroSource;
        private AudioSource oneShotSource;
        private bool nitroWasActive;
        private float nextImpactTime;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            driftClip ??= BuildNoiseLoop("3Fareet Drift Squeal", 1.0f, 980f, 1780f);
            nitroLoopClip ??= BuildNoiseLoop("3Fareet Nitro Rush", 1.0f, 120f, 640f);
            nitroBurstClip ??= BuildBurst("3Fareet Nitro Burst", .28f, 180f, 880f);
            impactClip ??= BuildImpact();

            var isPlayer = gameObject.name.Contains("PLAYER HERO");
            driftSource = CreateLoop("Drift SFX", driftClip, isPlayer);
            nitroSource = CreateLoop("Nitro SFX", nitroLoopClip, isPlayer);
            oneShotSource = CreateSource("Driving One Shots", isPlayer);
            driftSource.Play();
            nitroSource.Play();
        }

        private void Update()
        {
            if (car == null) return;

            var driftTarget = car.IsDrifting ? .16f : 0f;
            var nitroActive = car.NitroActive && car.NitroEnergy > .01f;
            var nitroTarget = nitroActive ? .18f : 0f;

            driftSource.volume = Mathf.MoveTowards(driftSource.volume, driftTarget, Time.unscaledDeltaTime * .75f);
            nitroSource.volume = Mathf.MoveTowards(nitroSource.volume, nitroTarget, Time.unscaledDeltaTime * 1.2f);
            driftSource.pitch = Mathf.Clamp(.90f + Mathf.Abs(car.SpeedKph) / 300f, .90f, 1.35f);
            nitroSource.pitch = Mathf.Clamp(.92f + Mathf.Abs(car.SpeedKph) / 360f, .92f, 1.35f);

            if (nitroActive && !nitroWasActive)
            {
                oneShotSource.pitch = 1f;
                oneShotSource.PlayOneShot(nitroBurstClip, .28f);
            }
            nitroWasActive = nitroActive;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (oneShotSource == null || collision == null || Time.unscaledTime < nextImpactTime) return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < 5.5f) return;

            nextImpactTime = Time.unscaledTime + .12f;
            var severity = Mathf.InverseLerp(5.5f, 28f, impactSpeed);
            oneShotSource.pitch = Mathf.Lerp(1.08f, .82f, severity);
            oneShotSource.PlayOneShot(impactClip, Mathf.Lerp(.16f, .48f, severity));
        }

        private AudioSource CreateLoop(string sourceName, AudioClip clip, bool isPlayer)
        {
            var source = CreateSource(sourceName, isPlayer);
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            return source;
        }

        private AudioSource CreateSource(string sourceName, bool isPlayer)
        {
            var host = new GameObject(sourceName);
            host.transform.SetParent(transform, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.spatialBlend = isPlayer ? .12f : 1f;
            source.minDistance = 3f;
            source.maxDistance = 36f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static AudioClip BuildNoiseLoop(string name, float duration, float lowHz, float highHz)
        {
            const int sampleRate = 22050;
            var count = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)sampleRate;
                var a = Mathf.Sin(2f * Mathf.PI * lowHz * t);
                var b = Mathf.Sin(2f * Mathf.PI * highHz * t + Mathf.Sin(t * 19f) * .65f);
                var c = Mathf.Sin(2f * Mathf.PI * (lowHz * 1.73f) * t);
                data[i] = Mathf.Clamp((a * .30f + b * .18f + c * .12f), -.72f, .72f);
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildBurst(string name, float duration, float lowHz, float highHz)
        {
            const int sampleRate = 22050;
            var count = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)sampleRate;
                var normalized = i / (float)count;
                var frequency = Mathf.Lerp(lowHz, highHz, normalized);
                var envelope = Mathf.Sin(Mathf.PI * normalized) * (1f - normalized * .35f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * .55f;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildImpact()
        {
            const int sampleRate = 22050;
            const float duration = .22f;
            var count = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)sampleRate;
                var normalized = i / (float)count;
                var envelope = Mathf.Exp(-normalized * 7f);
                var body = Mathf.Sin(2f * Mathf.PI * 82f * t) * .62f;
                var metal = Mathf.Sin(2f * Mathf.PI * 730f * t) * .24f + Mathf.Sin(2f * Mathf.PI * 1180f * t) * .12f;
                data[i] = Mathf.Clamp((body + metal) * envelope, -.86f, .86f);
            }
            var clip = AudioClip.Create("3Fareet Collision Impact", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
