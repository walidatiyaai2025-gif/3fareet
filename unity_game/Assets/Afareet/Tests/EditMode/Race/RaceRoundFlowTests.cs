using System;
using Afareet.Race;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class RaceRoundFlowTests
    {
        [Test]
        public void CountdownTransitionsToRacingOnlyAtZero()
        {
            var flow = new RaceRoundFlowState();
            flow.BeginCountdown(3f);

            Assert.That(flow.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(flow.TickCountdown(1f), Is.False);
            Assert.That(flow.CountdownRemaining, Is.EqualTo(2f).Within(.0001f));
            Assert.That(flow.TickCountdown(1.9f), Is.False);
            Assert.That(flow.TickCountdown(.1f), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(RaceRoundPhase.Racing));
            Assert.That(flow.CountdownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void ResultsRequireRacingAndRestartRequiresResults()
        {
            var flow = new RaceRoundFlowState();
            Assert.Throws<InvalidOperationException>(() => flow.Finish(4f));
            Assert.Throws<InvalidOperationException>(() => flow.Restart());

            flow.BeginCountdown(1f);
            flow.TickCountdown(1f);
            flow.Finish(4f);

            Assert.That(flow.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(flow.FinishTime, Is.EqualTo(4f));

            flow.Restart();
            Assert.That(flow.Phase, Is.EqualTo(RaceRoundPhase.Ready));
            Assert.That(flow.RoundNumber, Is.EqualTo(2));
            Assert.That(flow.FinishTime, Is.EqualTo(-1f));
        }

        [Test]
        public void RuntimeControllerCompletesCountdownRaceResultsAndRestart()
        {
            var racer = new GameObject("URAC-005 Racer");
            try
            {
                var checkpoints = racer.AddComponent<RacerCheckpointTracker>();
                var lap = racer.AddComponent<OneLapRaceTracker>();
                var controller = racer.AddComponent<RaceRoundController>();
                var startEvents = 0;
                var resultEvents = 0;
                var resetEvents = 0;
                var resultTime = -1f;
                controller.RaceStarted += () => startEvents++;
                controller.ResultsReady += time => { resultEvents++; resultTime = time; };
                controller.RoundReset += () => resetEvents++;

                controller.Configure(4, 3f);
                controller.BeginCountdown();
                Assert.That(controller.AdvanceCountdown(2f), Is.False);
                Assert.That(controller.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
                Assert.That(lap.IsStarted, Is.False);

                Assert.That(controller.AdvanceCountdown(1f), Is.True);
                Assert.That(controller.Phase, Is.EqualTo(RaceRoundPhase.Racing));
                Assert.That(lap.IsStarted, Is.True);
                Assert.That(startEvents, Is.EqualTo(1));

                lap.AdvanceTime(5f);
                checkpoints.TryPassCheckpoint(1);
                checkpoints.TryPassCheckpoint(2);
                checkpoints.TryPassCheckpoint(3);
                checkpoints.TryPassCheckpoint(0);

                Assert.That(controller.Phase, Is.EqualTo(RaceRoundPhase.Results));
                Assert.That(resultEvents, Is.EqualTo(1));
                Assert.That(resultTime, Is.EqualTo(5f).Within(.0001f));
                Assert.That(controller.FinishTime, Is.EqualTo(5f).Within(.0001f));

                controller.RestartRound();
                Assert.That(controller.Phase, Is.EqualTo(RaceRoundPhase.Ready));
                Assert.That(controller.RoundNumber, Is.EqualTo(2));
                Assert.That(resetEvents, Is.EqualTo(1));
                Assert.That(lap.IsStarted, Is.False);
                Assert.That(checkpoints.ExpectedCheckpointIndex, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(racer);
            }
        }

        [Test]
        public void InvalidCountdownDeltaIsRejected()
        {
            var flow = new RaceRoundFlowState();
            flow.BeginCountdown(3f);
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.TickCountdown(-.01f));
        }
    }
}
