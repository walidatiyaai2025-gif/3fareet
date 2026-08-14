using UnityEngine;

namespace Afareet.Core
{
    public sealed class CairoMusicLifecycle : MonoBehaviour
    {
        private static readonly int[] BassNotes = { 0, 0, 3, 2, 0, 5, 3, 2 };
        private static readonly int[] LeadNotes = { 0, 1, 4, 5, 4, 1, 0, -2 };
        private static CairoMusicLifecycle instance;
        private AudioSource source;
        private bool gamePaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null || FindFirstObjectByType<CairoMusicLifecycle>() != null) return;
            var host = new GameObject("3FAREET CAIRO MUSIC");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoMusicLifecycle>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = .30f;
            source.clip = BuildLoop();
            source.Play();
        }

        public static void SetGamePaused(bool paused)
        {
            if (instance == null || instance.source == null) return;
            instance.gamePaused = paused;
            if (paused) instance.source.Pause();
            else instance.source.UnPause();
        }

        public static void SetVolume(float volume)
        {
            if (instance != null && instance.source != null)
                instance.source.volume = Mathf.Clamp01(volume);
        }

        private void OnApplicationPause(bool paused)
        {
            if (source == null || gamePaused) return;
            if (paused) source.Pause();
            else source.UnPause();
        }

        private static AudioClip BuildLoop()
        {
            const int sampleRate = 22050;
            const float bpm = 100f;
            const int beats = 16;
            var beatSeconds = 60f / bpm;
            var sampleCount = Mathf.CeilToInt(beatSeconds * beats * sampleRate);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var beatPosition = t / beatSeconds;
                var beatIndex = Mathf.FloorToInt(beatPosition);
                var withinBeat = (beatPosition - beatIndex) * beatSeconds;

                var kick = Mathf.Sin(2f * Mathf.PI * 58f * t) * Mathf.Exp(-withinBeat * 18f) * .38f;
                var clapOn = beatIndex % 4 == 1 || beatIndex % 4 == 3;
                var clap = clapOn ? Mathf.Sin(2f * Mathf.PI * 1250f * t) * Mathf.Exp(-withinBeat * 30f) * .09f : 0f;

                var bassSemitone = BassNotes[beatIndex % BassNotes.Length] - 12;
                var bassHz = 220f * Mathf.Pow(2f, bassSemitone / 12f);
                var bass = Mathf.Sin(2f * Mathf.PI * bassHz * t) * .18f;

                var eighth = Mathf.FloorToInt(beatPosition * 2f);
                var leadSemitone = LeadNotes[eighth % LeadNotes.Length];
                var leadHz = 330f * Mathf.Pow(2f, leadSemitone / 12f);
                var lead = Mathf.Sin(2f * Mathf.PI * leadHz * t) * (eighth % 2 == 0 ? .11f : .07f);

                data[i] = Mathf.Clamp(kick + clap + bass + lead, -.9f, .9f);
            }

            var clip = AudioClip.Create("3Fareet Cairo Rap Shaabi Loop", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
