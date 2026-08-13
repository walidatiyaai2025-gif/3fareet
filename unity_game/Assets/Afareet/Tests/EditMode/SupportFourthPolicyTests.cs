using NUnit.Framework;

namespace Afareet.Tests.Support
{
    public sealed class SupportFourthPolicyTests
    {
        [Test] public void NumericWholeRounds() => Assert.That(Afareet.Support.NumericFormatPolicy.Whole(12.6f), Is.EqualTo(13));
        [Test] public void FovGrowsWithSpeed() => Assert.That(Afareet.Support.CameraFovResponsePolicy.Target(1f, 0f), Is.GreaterThan(Afareet.Support.CameraFovResponsePolicy.Target(0f, 0f)));
        [Test] public void ScalarBlendClamps() => Assert.That(Afareet.Support.ScalarBlendPolicy.Blend(0f, 10f, 2f), Is.EqualTo(10f));
        [Test] public void PauseLowersMix() => Assert.That(Afareet.Support.MixLevelPolicy.Primary(true), Is.LessThan(Afareet.Support.MixLevelPolicy.Primary(false)));
        [Test] public void CountdownShowsThree() => Assert.That(Afareet.Support.CountdownCuePolicy.VisibleNumber(2.8f), Is.EqualTo(3));
        [Test] public void PositionIsBounded() => Assert.That(Afareet.Support.PositionLabelPolicy.Format(9, 4), Is.EqualTo("4/4"));
        [Test] public void MetricScaleIsPositive() => Assert.That(Afareet.Support.SpeedScalePolicy.Display(10f, true), Is.GreaterThan(0f));
        [Test] public void ControlModeIsBounded() => Assert.That(Afareet.Support.ControlModePolicy.Normalize(8), Is.EqualTo(3));
        [Test] public void TierIsBounded() => Assert.That(Afareet.Support.TierIndexPolicy.Clamp(-1), Is.EqualTo(0));
        [Test] public void SessionSequenceWraps() => Assert.That(Afareet.Support.SessionSequencePolicy.Next(int.MaxValue), Is.EqualTo(1));
    }
}
