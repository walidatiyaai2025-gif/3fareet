using System;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class DriftScorePolicyTests
    {
        [Test]
        public void ScoreDelta_UsesSpeedSteerAndDurationOnlyWhileDrifting()
        {
            Assert.That(DriftScorePolicy.ScoreDelta(false, 100d, 1d, 1d), Is.Zero);
            Assert.That(DriftScorePolicy.ScoreDelta(true, 100d, 0d, 1d), Is.EqualTo(100d));
            Assert.That(DriftScorePolicy.ScoreDelta(true, 100d, 1d, 1d), Is.EqualTo(200d));
            Assert.That(DriftScorePolicy.ScoreDelta(true, -100d, .5d, 2d), Is.EqualTo(300d));
        }

        [Test]
        public void SixtySecondsAtTargetPace_ReachesChapterDriftTarget()
        {
            var score = DriftScorePolicy.ScoreDelta(true, 100d, 1d, 60d);
            Assert.That(score, Is.EqualTo(12000d));
        }

        [Test]
        public void InvalidNumericInputs_FailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DriftScorePolicy.ScoreDelta(true, double.NaN, 0d, 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => DriftScorePolicy.ScoreDelta(true, 10d, double.PositiveInfinity, 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => DriftScorePolicy.ScoreDelta(true, 10d, 0d, -1d));
        }
    }
}
