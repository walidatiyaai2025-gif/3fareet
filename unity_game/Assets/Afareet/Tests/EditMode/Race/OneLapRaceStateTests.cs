using System;
using Afareet.Race;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class OneLapRaceStateTests
    {
        [Test]
        public void StateStartsReadyAndOnlyAdvancesTimeWhileRacing()
        {
            var state = new OneLapRaceState(4);

            Assert.That(state.Phase, Is.EqualTo(OneLapRacePhase.Ready));
            state.Tick(2f);
            Assert.That(state.ElapsedTime, Is.EqualTo(0f));

            state.StartRace();
            state.Tick(1.25f);

            Assert.That(state.Phase, Is.EqualTo(OneLapRacePhase.Racing));
            Assert.That(state.ElapsedTime, Is.EqualTo(1.25f).Within(.0001f));
            Assert.That(state.CompletedLaps, Is.EqualTo(0));
        }

        [Test]
        public void SecondStartIsRejected()
        {
            var state = new OneLapRaceState(3);
            state.StartRace();

            Assert.Throws<InvalidOperationException>(() => state.StartRace());
        }

        [Test]
        public void RuntimeTrackerRejectsEarlyFinishGateAndFinishesAfterOrderedLap()
        {
            var racer = new GameObject("URAC-003 Racer");
            try
            {
                var checkpoints = racer.AddComponent<RacerCheckpointTracker>();
                var lap = racer.AddComponent<OneLapRaceTracker>();
                lap.Configure(4);
                lap.StartRace();
                lap.AdvanceTime(7.5f);

                Assert.That(checkpoints.ExpectedCheckpointIndex, Is.EqualTo(1));
                Assert.That(checkpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.OutOfOrder));
                Assert.That(lap.IsFinished, Is.False);

                Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
                Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Duplicate));
                Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
                Assert.That(checkpoints.TryPassCheckpoint(3), Is.EqualTo(CheckpointValidationResult.Accepted));
                Assert.That(lap.IsFinished, Is.False);

                Assert.That(checkpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));
                Assert.That(lap.IsFinished, Is.True);
                Assert.That(lap.Phase, Is.EqualTo(OneLapRacePhase.Finished));
                Assert.That(lap.CompletedLaps, Is.EqualTo(1));
                Assert.That(lap.FinishTime, Is.EqualTo(7.5f).Within(.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(racer);
            }
        }

        [Test]
        public void FinishEventFiresOnceAndClockFreezesAfterFinish()
        {
            var racer = new GameObject("URAC-003 Finish Racer");
            try
            {
                var checkpoints = racer.AddComponent<RacerCheckpointTracker>();
                var lap = racer.AddComponent<OneLapRaceTracker>();
                var finishCount = 0;
                var eventFinishTime = -1f;
                lap.RaceFinished += time =>
                {
                    finishCount++;
                    eventFinishTime = time;
                };

                lap.Configure(3);
                lap.StartRace();
                lap.AdvanceTime(4f);
                checkpoints.TryPassCheckpoint(1);
                checkpoints.TryPassCheckpoint(2);
                checkpoints.TryPassCheckpoint(0);

                lap.AdvanceTime(9f);
                checkpoints.TryPassCheckpoint(1);

                Assert.That(finishCount, Is.EqualTo(1));
                Assert.That(eventFinishTime, Is.EqualTo(4f).Within(.0001f));
                Assert.That(lap.ElapsedTime, Is.EqualTo(4f).Within(.0001f));
                Assert.That(lap.FinishTime, Is.EqualTo(4f).Within(.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(racer);
            }
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OneLapRaceState(1));

            var racer = new GameObject("URAC-003 Invalid Racer");
            try
            {
                racer.AddComponent<RacerCheckpointTracker>();
                var lap = racer.AddComponent<OneLapRaceTracker>();
                Assert.Throws<ArgumentOutOfRangeException>(() => lap.Configure(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(racer);
            }
        }
    }
}
