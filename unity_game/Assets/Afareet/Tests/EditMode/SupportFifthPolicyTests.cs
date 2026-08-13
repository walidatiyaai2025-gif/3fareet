using NUnit.Framework;

namespace Afareet.Tests.Support
{
    public sealed class SupportFifthPolicyTests
    {
        [Test] public void ValueRangeClamps() => Assert.That(Afareet.Support.ValueRangePolicy.Limit(9f, 0f, 5f), Is.EqualTo(5f));
        [Test] public void IntBandSelectsHigh() => Assert.That(Afareet.Support.IntBandPolicy.Select(10, 4, 8), Is.EqualTo(2));
        [Test] public void LightCountScales() => Assert.That(Afareet.Support.LightCountPolicy.MaxCount(2), Is.EqualTo(6));
        [Test] public void StepRateUsesLowTierTarget() => Assert.That(Afareet.Support.StepRatePolicy.TargetMs(0), Is.EqualTo(33));
        [Test] public void RestartTokenAdvances() => Assert.That(Afareet.Support.RestartTokenPolicy.Next(4), Is.EqualTo(5));
        [Test] public void LocaleIndexFallsBack() => Assert.That(Afareet.Support.LocaleIndexPolicy.Select(9, 2), Is.EqualTo(0));
        [Test] public void PercentClampsHigh() => Assert.That(Afareet.Support.PercentIntPolicy.Clamp(150), Is.EqualTo(100));
        [Test] public void FeatureTierHonorsRequirement() => Assert.That(Afareet.Support.FeatureTierPolicy.Enabled(2, 1), Is.True);
        [Test] public void SequenceWraps() => Assert.That(Afareet.Support.SequenceIndexPolicy.Wrap(5, 3), Is.EqualTo(2));
        [Test] public void TickWindowContainsInteriorTick() => Assert.That(Afareet.Support.TickWindowPolicy.Contains(12, 10, 5), Is.True);
    }
}
