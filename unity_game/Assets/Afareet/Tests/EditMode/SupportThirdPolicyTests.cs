using NUnit.Framework;

namespace Afareet.Tests.Support
{
    public sealed class SupportThirdPolicyTests
    {
        [Test] public void CameraPullsForwardForObstruction() => Assert.That(Afareet.Support.CameraObstructionPolicy.ResolveDistance(6f, 3f), Is.LessThan(6f));
        [Test] public void HudTelemetryClampsValues() => Assert.That(Afareet.Support.HudTelemetryPolicy.Normalize(0, -5f, 2f, -1f).Position, Is.EqualTo(1));
        [Test] public void FrameBudgetAcceptsThirtyFpsSample() => Assert.That(Afareet.Support.FrameBudgetPolicy.MeetsTarget(30f, 30f), Is.True);
        [Test] public void AndroidIdentityPolicyAcceptsExpectedShape() => Assert.That(Afareet.Support.AndroidArtifactPolicy.Accept("com.fiftysolutions.afareetunity3d", "3Fareet", true, true), Is.True);
        [Test] public void DeviceSmokeRequiresAllChecks() => Assert.That(new Afareet.Support.DeviceSmokeResult(true, true, true, true).Passed, Is.True);
        [Test] public void ReadabilityThresholdIsExplicit() => Assert.That(Afareet.Support.ReadabilityPolicy.Pass(.8f), Is.True);
        [Test] public void ResourceTrendAllowsSmallGrowth() => Assert.That(Afareet.Support.ResourceTrendPolicy.Stable(10f, 11f, 1f), Is.True);
        [Test] public void RenderWorkHonorsLimit() => Assert.That(Afareet.Support.RenderWorkPolicy.WithinBudget(80, 100), Is.True);
        [Test] public void EffectBudgetStopsAtLimit() => Assert.That(Afareet.Support.EffectBudgetPolicy.Allow(4, 4), Is.False);
        [Test] public void EvidenceGateNeedsEveryGate() => Assert.That(Afareet.Support.EvidenceGatePolicy.Complete(true, true, true, false), Is.False);
    }
}
