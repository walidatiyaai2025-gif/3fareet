using Afareet.Race;
using Afareet.UI;
using NUnit.Framework;

namespace Afareet.Tests.UI
{
    public sealed class RaceUiPresentationPolicyTests
    {
        [Test]
        public void PauseOverlayAppearsOnlyWhileRacingAndPaused()
        {
            Assert.AreEqual(RaceOverlayMode.Pause, RaceUiPresentationPolicy.Resolve(RaceRoundPhase.Racing, true));
            Assert.AreEqual(RaceOverlayMode.Hidden, RaceUiPresentationPolicy.Resolve(RaceRoundPhase.Racing, false));
            Assert.AreEqual(RaceOverlayMode.Hidden, RaceUiPresentationPolicy.Resolve(RaceRoundPhase.Countdown, true));
        }

        [Test]
        public void ResultsOverlayHasPriorityOverPauseFlag()
        {
            Assert.AreEqual(RaceOverlayMode.Results, RaceUiPresentationPolicy.Resolve(RaceRoundPhase.Results, false));
            Assert.AreEqual(RaceOverlayMode.Results, RaceUiPresentationPolicy.Resolve(RaceRoundPhase.Results, true));
        }

        [Test]
        public void PauseResumeAndRestartActionsArePhaseGuarded()
        {
            Assert.IsTrue(RaceUiPresentationPolicy.CanPause(RaceRoundPhase.Racing, false));
            Assert.IsFalse(RaceUiPresentationPolicy.CanPause(RaceRoundPhase.Countdown, false));
            Assert.IsTrue(RaceUiPresentationPolicy.CanResume(RaceRoundPhase.Racing, true));
            Assert.IsFalse(RaceUiPresentationPolicy.CanResume(RaceRoundPhase.Results, true));
            Assert.IsTrue(RaceUiPresentationPolicy.CanRestart(RaceRoundPhase.Results));
            Assert.IsFalse(RaceUiPresentationPolicy.CanRestart(RaceRoundPhase.Racing));
        }

        [Test]
        public void ReviewedRoundFlowTransitionsToResultsAndRestart()
        {
            var flow = new RaceRoundFlowState();
            flow.BeginCountdown(3f);

            Assert.AreEqual(RaceRoundPhase.Countdown, flow.Phase);
            Assert.IsFalse(flow.TickCountdown(1f));
            Assert.IsTrue(flow.TickCountdown(2f));
            Assert.AreEqual(RaceRoundPhase.Racing, flow.Phase);

            flow.Finish(12.5f);
            Assert.AreEqual(RaceRoundPhase.Results, flow.Phase);
            Assert.AreEqual(12.5f, flow.FinishTime, 0.001f);

            flow.Restart();
            Assert.AreEqual(RaceRoundPhase.Ready, flow.Phase);
            Assert.AreEqual(2, flow.RoundNumber);
            Assert.AreEqual(-1f, flow.FinishTime, 0.001f);
        }

        [Test]
        public void OrderedCheckpointContractRejectsSkippedCheckpoint()
        {
            var checkpoints = new OrderedCheckpointValidator(4, firstExpectedCheckpointIndex: 1);
            Assert.AreEqual(CheckpointValidationResult.OutOfOrder, checkpoints.TryAccept(2));
            Assert.AreEqual(CheckpointValidationResult.Accepted, checkpoints.TryAccept(1));
            Assert.AreEqual(CheckpointValidationResult.Accepted, checkpoints.TryAccept(2));
        }
    }
}
