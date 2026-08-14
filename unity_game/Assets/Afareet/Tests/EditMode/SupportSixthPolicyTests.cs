using NUnit.Framework;

namespace Afareet.Tests.Support
{
    public sealed class SupportSixthPolicyTests
    {
        [Test]
        public void BoundedCounterClampsAtMaximum()
        {
            Assert.That(Afareet.Support.BoundedCounterPolicy.Add(8, 5, 0, 10), Is.EqualTo(10));
        }

        [Test]
        public void CycleSelectorWrapsAtCount()
        {
            Assert.That(Afareet.Support.CycleSelectorPolicy.Next(2, 3), Is.EqualTo(0));
        }

        [Test]
        public void FlagGateOpensOnlyForActiveIncompleteUnpausedState()
        {
            Assert.That(Afareet.Support.FlagGatePolicy.Open(true, false, false), Is.True);
        }

        [Test]
        public void GapPolicyAcceptsConfiguredDistance()
        {
            Assert.That(Afareet.Support.GapIntPolicy.HasGap(10, 6, 4), Is.True);
        }

        [Test]
        public void HysteresisRetainsActiveStateInsideBand()
        {
            Assert.That(Afareet.Support.HysteresisIntPolicy.Next(5, 1, 3, 7), Is.EqualTo(1));
        }

        [Test]
        public void MinimumCountAcceptsExactRequirement()
        {
            Assert.That(Afareet.Support.MinimumCountPolicy.Enough(3, 3), Is.True);
        }

        [Test]
        public void SampleWindowReturnsRollingStartIndex()
        {
            Assert.That(Afareet.Support.SampleWindowPolicy.Start(7, 3), Is.EqualTo(5));
        }

        [Test]
        public void StateLatchClearSignalWins()
        {
            Assert.That(Afareet.Support.StateLatchPolicy.Next(true, true, true), Is.False);
        }

        [Test]
        public void TolerancePolicyAcceptsBoundaryDelta()
        {
            Assert.That(Afareet.Support.ToleranceIntPolicy.Within(102, 100, 2), Is.True);
        }

        [Test]
        public void WeightRatioCapsAtOne()
        {
            Assert.That(Afareet.Support.WeightRatioPolicy.Ratio(12f, 10f), Is.EqualTo(1f));
        }
    }
}
