using System;
using System.Collections.Generic;
using Afareet.Race;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class RaceRankingTests
    {
        [Test]
        public void AcceptedCheckpointCountBeatsSegmentProximity()
        {
            var rankings = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("near-future-waypoint", checkpoints: 1, segment: .99f, stable: 0),
                Progress("validated-ahead", checkpoints: 2, segment: .05f, stable: 1)
            });

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("validated-ahead"));
            Assert.That(rankings[0].Position, Is.EqualTo(1));
        }

        [Test]
        public void SegmentProgressOnlyBreaksTieWithinSameValidatedCheckpoint()
        {
            var rankings = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("segment-20", checkpoints: 3, segment: .20f, stable: 0),
                Progress("segment-80", checkpoints: 3, segment: .80f, stable: 1)
            });

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("segment-80"));
        }

        [Test]
        public void CompletedLapBeatsAnyCurrentLapCheckpointProgress()
        {
            var rankings = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("current-lap", laps: 0, checkpoints: 99, segment: 1f, stable: 0),
                Progress("lap-ahead", laps: 1, checkpoints: 0, segment: 0f, stable: 1)
            });

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("lap-ahead"));
        }

        [Test]
        public void FinishedRacersLeadAndLowerFinishTimeWins()
        {
            var rankings = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("not-finished", checkpoints: 50, segment: 1f, stable: 0),
                Finished("second", 12f, 1),
                Finished("winner", 10f, 2)
            });

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("winner"));
            Assert.That(rankings[1].Progress.RacerId, Is.EqualTo("second"));
            Assert.That(rankings[2].Progress.RacerId, Is.EqualTo("not-finished"));
        }

        [Test]
        public void StableOrderMakesExactTiesDeterministic()
        {
            var rankings = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("registered-second", checkpoints: 2, segment: .5f, stable: 1),
                Progress("registered-first", checkpoints: 2, segment: .5f, stable: 0)
            });

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("registered-first"));
        }

        [Test]
        public void RankDoesNotMutateCallerInputOrder()
        {
            var input = new List<RaceProgressSnapshot>
            {
                Progress("behind", checkpoints: 1, segment: .2f, stable: 0),
                Progress("ahead", checkpoints: 3, segment: .8f, stable: 1),
                Progress("middle", checkpoints: 2, segment: .5f, stable: 2)
            };

            var rankings = RaceRanking.Rank(input);

            Assert.That(rankings[0].Progress.RacerId, Is.EqualTo("ahead"));
            Assert.That(rankings[1].Progress.RacerId, Is.EqualTo("middle"));
            Assert.That(rankings[2].Progress.RacerId, Is.EqualTo("behind"));
            Assert.That(input[0].RacerId, Is.EqualTo("behind"));
            Assert.That(input[1].RacerId, Is.EqualTo("ahead"));
            Assert.That(input[2].RacerId, Is.EqualTo("middle"));
        }

        [Test]
        public void DuplicateRacerIdsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("duplicate", stable: 0),
                Progress("duplicate", stable: 1)
            }));
        }

        [Test]
        public void CaptureReadsValidatedCheckpointAndLapState()
        {
            var racer = new GameObject("URAC-004 Racer");
            try
            {
                var checkpoints = racer.AddComponent<RacerCheckpointTracker>();
                var lap = racer.AddComponent<OneLapRaceTracker>();
                lap.Configure(4);
                lap.StartRace();
                Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));

                var snapshot = RaceRanking.Capture("player", checkpoints, lap, .4f, 0);

                Assert.That(snapshot.AcceptedCheckpoints, Is.EqualTo(1));
                Assert.That(snapshot.CompletedLaps, Is.EqualTo(0));
                Assert.That(snapshot.SegmentProgress, Is.EqualTo(.4f).Within(.0001f));
                Assert.That(snapshot.IsFinished, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(racer);
            }
        }

        [Test]
        public void InvalidSegmentProgressIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Progress("bad-low", segment: -.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Progress("bad-high", segment: 1.01f));
        }

        private static RaceProgressSnapshot Progress(string id, int laps = 0, int checkpoints = 0, float segment = 0f, int stable = 0)
        {
            return new RaceProgressSnapshot(id, false, laps, checkpoints, segment, -1f, stable);
        }

        private static RaceProgressSnapshot Finished(string id, float finishTime, int stable)
        {
            return new RaceProgressSnapshot(id, true, 1, 4, 1f, finishTime, stable);
        }
    }
}
