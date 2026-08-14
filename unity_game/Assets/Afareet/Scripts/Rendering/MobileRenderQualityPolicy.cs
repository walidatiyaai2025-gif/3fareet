using System;

namespace Afareet.Rendering
{
    public enum MobileRenderTier
    {
        Low,
        Mid,
        High
    }

    [Serializable]
    public sealed class MobileRenderSelection
    {
        public int lowBelowSystemMemoryMb;
        public int highAtOrAboveSystemMemoryMb;
        public int lowBelowGraphicsMemoryMb;
        public int highAtOrAboveGraphicsMemoryMb;
        public int lowBelowShaderLevel;
        public int highAtOrAboveShaderLevel;
    }

    [Serializable]
    public sealed class MobileRenderProfile
    {
        public int targetFps;
        public float renderScale;
        public int pixelLightCount;
        public float shadowDistanceMeters;
        public bool softShadows;
        public int shadowCascades;
        public int shadowResolution;
        public int antiAliasing;
        public int anisotropicMode;
        public float lodBias;
        public int maximumLodLevel;
        public int shaderMaximumLod;
        public bool softParticles;
        public bool realtimeReflectionProbes;
    }

    [Serializable]
    public sealed class MobileRenderQualityConfig
    {
        public int schemaVersion;
        public MobileRenderSelection selection;
        public MobileRenderProfile low;
        public MobileRenderProfile mid;
        public MobileRenderProfile high;

        public MobileRenderProfile Profile(MobileRenderTier tier) => tier switch
        {
            MobileRenderTier.Low => low,
            MobileRenderTier.Mid => mid,
            MobileRenderTier.High => high,
            _ => throw new ArgumentOutOfRangeException(nameof(tier))
        };
    }

    public static class MobileRenderQualityPolicy
    {
        public const string ResourcePath = "Config/runtime_builtin_quality_tiers";
        public const string OverridePlayerPrefsKey = "afareet.render_quality";

        public static MobileRenderTier Select(
            MobileRenderSelection selection,
            int systemMemoryMb,
            int graphicsMemoryMb,
            int shaderLevel)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            ValidateSelection(selection);

            if (KnownBelow(systemMemoryMb, selection.lowBelowSystemMemoryMb) ||
                KnownBelow(graphicsMemoryMb, selection.lowBelowGraphicsMemoryMb) ||
                KnownBelow(shaderLevel, selection.lowBelowShaderLevel))
                return MobileRenderTier.Low;

            if (KnownAtLeast(systemMemoryMb, selection.highAtOrAboveSystemMemoryMb) &&
                KnownAtLeast(graphicsMemoryMb, selection.highAtOrAboveGraphicsMemoryMb) &&
                KnownAtLeast(shaderLevel, selection.highAtOrAboveShaderLevel))
                return MobileRenderTier.High;

            return MobileRenderTier.Mid;
        }

        public static bool TryParseOverride(string value, out MobileRenderTier tier)
        {
            if (string.Equals(value, "low", StringComparison.OrdinalIgnoreCase))
            {
                tier = MobileRenderTier.Low;
                return true;
            }
            if (string.Equals(value, "mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "medium", StringComparison.OrdinalIgnoreCase))
            {
                tier = MobileRenderTier.Mid;
                return true;
            }
            if (string.Equals(value, "high", StringComparison.OrdinalIgnoreCase))
            {
                tier = MobileRenderTier.High;
                return true;
            }

            tier = MobileRenderTier.Mid;
            return false;
        }

        public static void ValidateConfig(MobileRenderQualityConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.schemaVersion != 1) throw new InvalidOperationException("Unsupported render-quality schema version.");
            ValidateSelection(config.selection);
            ValidateProfile("low", config.low);
            ValidateProfile("mid", config.mid);
            ValidateProfile("high", config.high);

            if (!(config.low.targetFps <= config.mid.targetFps && config.mid.targetFps <= config.high.targetFps))
                throw new InvalidOperationException("Render target FPS must be monotonic Low <= Mid <= High.");
            if (!(config.low.renderScale <= config.mid.renderScale && config.mid.renderScale <= config.high.renderScale))
                throw new InvalidOperationException("Render scale must be monotonic Low <= Mid <= High.");
            if (!(config.low.pixelLightCount <= config.mid.pixelLightCount && config.mid.pixelLightCount <= config.high.pixelLightCount))
                throw new InvalidOperationException("Pixel-light budget must be monotonic Low <= Mid <= High.");
            if (!(config.low.shadowDistanceMeters <= config.mid.shadowDistanceMeters && config.mid.shadowDistanceMeters <= config.high.shadowDistanceMeters))
                throw new InvalidOperationException("Shadow distance must be monotonic Low <= Mid <= High.");
            if (!(config.low.lodBias <= config.mid.lodBias && config.mid.lodBias <= config.high.lodBias))
                throw new InvalidOperationException("LOD bias must be monotonic Low <= Mid <= High.");
        }

        private static void ValidateSelection(MobileRenderSelection selection)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (selection.lowBelowSystemMemoryMb <= 0 ||
                selection.highAtOrAboveSystemMemoryMb <= selection.lowBelowSystemMemoryMb)
                throw new InvalidOperationException("System-memory tier thresholds are invalid.");
            if (selection.lowBelowGraphicsMemoryMb <= 0 ||
                selection.highAtOrAboveGraphicsMemoryMb <= selection.lowBelowGraphicsMemoryMb)
                throw new InvalidOperationException("Graphics-memory tier thresholds are invalid.");
            if (selection.lowBelowShaderLevel <= 0 ||
                selection.highAtOrAboveShaderLevel <= selection.lowBelowShaderLevel)
                throw new InvalidOperationException("Shader-level tier thresholds are invalid.");
        }

        private static void ValidateProfile(string name, MobileRenderProfile profile)
        {
            if (profile == null) throw new InvalidOperationException($"Render profile '{name}' is missing.");
            if (profile.targetFps < 20 || profile.targetFps > 240) throw new InvalidOperationException($"{name}: targetFps is invalid.");
            if (profile.renderScale < .5f || profile.renderScale > 1f) throw new InvalidOperationException($"{name}: renderScale is invalid.");
            if (profile.pixelLightCount < 0 || profile.pixelLightCount > 8) throw new InvalidOperationException($"{name}: pixelLightCount is invalid.");
            if (profile.shadowDistanceMeters < 0f || profile.shadowDistanceMeters > 150f) throw new InvalidOperationException($"{name}: shadowDistanceMeters is invalid.");
            if (profile.shadowCascades != 0 && profile.shadowCascades != 2 && profile.shadowCascades != 4) throw new InvalidOperationException($"{name}: shadowCascades must be 0, 2 or 4.");
            if (profile.shadowResolution < 0 || profile.shadowResolution > 3) throw new InvalidOperationException($"{name}: shadowResolution is invalid.");
            if (profile.antiAliasing != 0 && profile.antiAliasing != 2 && profile.antiAliasing != 4 && profile.antiAliasing != 8) throw new InvalidOperationException($"{name}: antiAliasing is invalid.");
            if (profile.anisotropicMode < 0 || profile.anisotropicMode > 2) throw new InvalidOperationException($"{name}: anisotropicMode is invalid.");
            if (profile.lodBias < .25f || profile.lodBias > 2f) throw new InvalidOperationException($"{name}: lodBias is invalid.");
            if (profile.maximumLodLevel < 0 || profile.maximumLodLevel > 2) throw new InvalidOperationException($"{name}: maximumLodLevel is invalid.");
            if (profile.shaderMaximumLod < 100 || profile.shaderMaximumLod > 1000) throw new InvalidOperationException($"{name}: shaderMaximumLod is invalid.");
        }

        private static bool KnownBelow(int value, int threshold) => value > 0 && value < threshold;
        private static bool KnownAtLeast(int value, int threshold) => value > 0 && value >= threshold;
    }
}
