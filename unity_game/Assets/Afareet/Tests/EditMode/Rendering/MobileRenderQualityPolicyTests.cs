using System;
using Afareet.Rendering;
using NUnit.Framework;

namespace Afareet.Tests.Rendering
{
    public sealed class MobileRenderQualityPolicyTests
    {
        [Test]
        public void LowMemorySelectsLowTier()
        {
            Assert.That(MobileRenderQualityPolicy.Select(Selection(), 3072, 4096, 50), Is.EqualTo(MobileRenderTier.Low));
        }

        [Test]
        public void LowGraphicsMemorySelectsLowTier()
        {
            Assert.That(MobileRenderQualityPolicy.Select(Selection(), 8192, 768, 50), Is.EqualTo(MobileRenderTier.Low));
        }

        [Test]
        public void StrongKnownDeviceSelectsHighTier()
        {
            Assert.That(MobileRenderQualityPolicy.Select(Selection(), 8192, 4096, 50), Is.EqualTo(MobileRenderTier.High));
        }

        [Test]
        public void UnknownOrMixedCapabilitiesStayMidTier()
        {
            Assert.That(MobileRenderQualityPolicy.Select(Selection(), 0, 0, 0), Is.EqualTo(MobileRenderTier.Mid));
            Assert.That(MobileRenderQualityPolicy.Select(Selection(), 8192, 1536, 50), Is.EqualTo(MobileRenderTier.Mid));
        }

        [TestCase("low", MobileRenderTier.Low)]
        [TestCase("MID", MobileRenderTier.Mid)]
        [TestCase("medium", MobileRenderTier.Mid)]
        [TestCase("High", MobileRenderTier.High)]
        public void QaOverrideParsesKnownTierNames(string value, MobileRenderTier expected)
        {
            Assert.That(MobileRenderQualityPolicy.TryParseOverride(value, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(expected));
        }

        [Test]
        public void AutoOverrideValueDoesNotForceTier()
        {
            Assert.That(MobileRenderQualityPolicy.TryParseOverride("auto", out _), Is.False);
        }

        [Test]
        public void ValidProductionStyleConfigPassesValidation()
        {
            Assert.DoesNotThrow(() => MobileRenderQualityPolicy.ValidateConfig(ValidConfig()));
        }

        [Test]
        public void NonMonotonicRenderScaleIsRejected()
        {
            var config = ValidConfig();
            config.low.renderScale = 1f;
            Assert.Throws<InvalidOperationException>(() => MobileRenderQualityPolicy.ValidateConfig(config));
        }

        [Test]
        public void InvalidAntiAliasingValueIsRejected()
        {
            var config = ValidConfig();
            config.mid.antiAliasing = 3;
            Assert.Throws<InvalidOperationException>(() => MobileRenderQualityPolicy.ValidateConfig(config));
        }

        private static MobileRenderSelection Selection() => new()
        {
            lowBelowSystemMemoryMb = 4096,
            highAtOrAboveSystemMemoryMb = 6144,
            lowBelowGraphicsMemoryMb = 1024,
            highAtOrAboveGraphicsMemoryMb = 2048,
            lowBelowShaderLevel = 35,
            highAtOrAboveShaderLevel = 45
        };

        private static MobileRenderQualityConfig ValidConfig() => new()
        {
            schemaVersion = 1,
            selection = Selection(),
            low = Profile(30, .8f, 2, 35f, false, 0, 0, 0, 0, .75f, 1, 150, false, false),
            mid = Profile(45, .9f, 4, 55f, true, 2, 1, 2, 1, 1f, 0, 200, true, false),
            high = Profile(60, 1f, 6, 75f, true, 4, 2, 4, 2, 1.25f, 0, 300, true, true)
        };

        private static MobileRenderProfile Profile(
            int fps,
            float scale,
            int lights,
            float shadowDistance,
            bool softShadows,
            int cascades,
            int shadowResolution,
            int aa,
            int anisotropic,
            float lodBias,
            int maximumLodLevel,
            int shaderMaximumLod,
            bool softParticles,
            bool reflections) => new()
        {
            targetFps = fps,
            renderScale = scale,
            pixelLightCount = lights,
            shadowDistanceMeters = shadowDistance,
            softShadows = softShadows,
            shadowCascades = cascades,
            shadowResolution = shadowResolution,
            antiAliasing = aa,
            anisotropicMode = anisotropic,
            lodBias = lodBias,
            maximumLodLevel = maximumLodLevel,
            shaderMaximumLod = shaderMaximumLod,
            softParticles = softParticles,
            realtimeReflectionProbes = reflections
        };
    }
}
