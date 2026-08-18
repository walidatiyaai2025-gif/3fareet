using System;
using System.Collections.Generic;
using Afareet.Race;
using UnityEngine;

namespace Afareet.Core
{
    public sealed class PowerUpProductionAudioBridge : MonoBehaviour
    {
        private static PowerUpProductionAudioBridge instance;
        private readonly Dictionary<string, AudioClip> clips = new(StringComparer.Ordinal);
        private readonly HashSet<string> missingReported = new(StringComparer.Ordinal);
        private AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null || FindFirstObjectByType<PowerUpProductionAudioBridge>() != null) return;
            var host = new GameObject("AFAREET POWER-UP PRODUCTION AUDIO BRIDGE");
            DontDestroyOnLoad(host);
            host.AddComponent<PowerUpProductionAudioBridge>();
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
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = .85f;
            PowerUpPresentationHub.Published += OnPresentation;
        }

        private void OnDestroy()
        {
            PowerUpPresentationHub.Published -= OnPresentation;
            if (instance == this) instance = null;
        }

        private void OnPresentation(RacerPowerUpPresentationEvent envelope)
        {
            if (envelope == null || envelope.Event == null || source == null) return;
            if (!TryResolveResourcePath(envelope.Event, out var resourcePath)) return;

            if (!clips.TryGetValue(resourcePath, out var clip))
            {
                clip = Resources.Load<AudioClip>(resourcePath);
                if (clip == null)
                {
                    ReportMissingOnce(resourcePath, envelope);
                    return;
                }
                clips.Add(resourcePath, clip);
            }

            source.PlayOneShot(clip);
        }

        public static bool TryResolveResourcePath(PowerUpPresentationEvent presentationEvent, out string resourcePath)
        {
            if (presentationEvent == null) throw new ArgumentNullException(nameof(presentationEvent));

            if (presentationEvent.EventKind == PowerUpPresentationEventKind.RaceReset ||
                presentationEvent.EventKind == PowerUpPresentationEventKind.Expired)
            {
                resourcePath = null;
                return false;
            }

            if (presentationEvent.EventKind == PowerUpPresentationEventKind.Blocked)
            {
                resourcePath = "Audio/Production/Sfx/PowerUps/SFX_EyeShield_Block";
                return true;
            }

            if (!presentationEvent.Kind.HasValue)
            {
                resourcePath = null;
                return false;
            }

            resourcePath = presentationEvent.Kind.Value switch
            {
                PowerUpKind.AsphaltShard => "Audio/Production/Sfx/PowerUps/SFX_AsphaltShard_Activate",
                PowerUpKind.NitroSpirit => "Audio/Production/Sfx/PowerUps/SFX_NitroSpirit_Activate",
                PowerUpKind.TrafficCurse => "Audio/Production/Sfx/PowerUps/SFX_TrafficCurse_Activate",
                PowerUpKind.EnchantedPound => "Audio/Production/Sfx/PowerUps/SFX_EnchantedPound_Activate",
                PowerUpKind.EyeShield => "Audio/Production/Sfx/PowerUps/SFX_EyeShield_Activate",
                _ => null
            };
            return resourcePath != null;
        }

        private void ReportMissingOnce(string resourcePath, RacerPowerUpPresentationEvent envelope)
        {
            if (!missingReported.Add(resourcePath)) return;

#if UNITY_EDITOR || AFAREET_EXPERIMENTAL_APK
            Debug.LogWarning(
                $"AFAREET_AUDIO_POWERUP_PRODUCTION_MISSING resource={resourcePath} " +
                $"racer={envelope.RacerId} event={envelope.Event.EventKind} production=false " +
                "syntheticFallback=false request=EXT-ASSET-004");
#else
            Debug.LogError(
                $"AFAREET_AUDIO_POWERUP_PRODUCTION_REQUIRED resource={resourcePath} " +
                $"event={envelope.Event.EventKind} syntheticFallback=false request=EXT-ASSET-004");
#endif
        }
    }
}
