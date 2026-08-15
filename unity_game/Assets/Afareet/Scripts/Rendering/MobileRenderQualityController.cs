using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afareet.Rendering
{
    public sealed class MobileRenderQualityController : MonoBehaviour
    {
        private MobileRenderQualityConfig config;
        private MobileRenderProfile activeProfile;

        public static MobileRenderTier CurrentTier { get; private set; } = MobileRenderTier.Mid;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<MobileRenderQualityController>() != null) return;
            var host = new GameObject("AFAREET BUILT-IN RENDER QUALITY");
            DontDestroyOnLoad(host);
            host.AddComponent<MobileRenderQualityController>();
        }

        public static void SetQaOverride(MobileRenderTier? tier)
        {
            if (tier.HasValue)
                PlayerPrefs.SetString(MobileRenderQualityPolicy.OverridePlayerPrefsKey, tier.Value.ToString().ToLowerInvariant());
            else
                PlayerPrefs.DeleteKey(MobileRenderQualityPolicy.OverridePlayerPrefsKey);
            PlayerPrefs.Save();

            var controller = FindFirstObjectByType<MobileRenderQualityController>();
            if (controller != null) controller.LoadSelectAndApply();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            LoadSelectAndApply();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && activeProfile != null) Apply(CurrentTier, activeProfile);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (activeProfile != null) ConfigureCameras(activeProfile);
        }

        private void LoadSelectAndApply()
        {
            var asset = Resources.Load<TextAsset>(MobileRenderQualityPolicy.ResourcePath);
            if (asset == null)
                throw new MissingReferenceException($"Required render-quality config is missing at Resources/{MobileRenderQualityPolicy.ResourcePath}.json");

            config = JsonUtility.FromJson<MobileRenderQualityConfig>(asset.text);
            MobileRenderQualityPolicy.ValidateConfig(config);

            var storedOverride = PlayerPrefs.GetString(MobileRenderQualityPolicy.OverridePlayerPrefsKey, string.Empty);
            if (MobileRenderQualityPolicy.TryParseOverride(storedOverride, out var forcedTier))
                CurrentTier = forcedTier;
            else
                CurrentTier = MobileRenderQualityPolicy.Select(
                    config.selection,
                    SystemInfo.systemMemorySize,
                    SystemInfo.graphicsMemorySize,
                    SystemInfo.graphicsShaderLevel);

            activeProfile = config.Profile(CurrentTier);
            Apply(CurrentTier, activeProfile);
        }

        private static void Apply(MobileRenderTier tier, MobileRenderProfile profile)
        {
            Application.targetFrameRate = profile.targetFps;
            QualitySettings.vSyncCount = 0;
            QualitySettings.pixelLightCount = profile.pixelLightCount;
            QualitySettings.shadowDistance = profile.shadowDistanceMeters;
            QualitySettings.shadows = profile.shadowDistanceMeters <= 0f
                ? ShadowQuality.Disable
                : profile.softShadows ? ShadowQuality.All : ShadowQuality.HardOnly;
            QualitySettings.shadowCascades = profile.shadowCascades;
            QualitySettings.shadowResolution = (ShadowResolution)profile.shadowResolution;
            QualitySettings.antiAliasing = profile.antiAliasing;
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)profile.anisotropicMode;
            QualitySettings.lodBias = profile.lodBias;
            QualitySettings.maximumLODLevel = profile.maximumLodLevel;
            QualitySettings.softParticles = profile.softParticles;
            QualitySettings.realtimeReflectionProbes = profile.realtimeReflectionProbes;
            Shader.globalMaximumLOD = profile.shaderMaximumLod;

            ScalableBufferManager.ResizeBuffers(profile.renderScale, profile.renderScale);
            ConfigureCameras(profile);

            Debug.Log(
                $"AFAREET_RENDER_QUALITY tier={tier} fps={profile.targetFps} scale={profile.renderScale:0.00} " +
                $"lights={profile.pixelLightCount} shadows={profile.shadowDistanceMeters:0}m " +
                $"aa={profile.antiAliasing} lodBias={profile.lodBias:0.00} shaderLod={profile.shaderMaximumLod}");
        }

        private static void ConfigureCameras(MobileRenderProfile profile)
        {
            foreach (var camera in Camera.allCameras)
            {
                if (camera == null) continue;
                camera.allowDynamicResolution = profile.renderScale < .999f;
            }
        }
    }
}
