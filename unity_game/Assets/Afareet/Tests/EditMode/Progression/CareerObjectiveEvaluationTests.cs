using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Afareet.Progression.Tests
{
    public sealed class CareerObjectiveEvaluationTests
    {
        [Test]
        public void FinishedCircuit_CompletesFinishObjective()
        {
            var result = Evaluate("c01_r01", new CareerEventOutcome(true, 0));
            Assert.That(result.Entries.Count, Is.EqualTo(1));
            Assert.That(result.CompletedCount, Is.EqualTo(1));
            Assert.That(result.AllCompleted, Is.True);
        }

        [Test]
        public void TimeTrial_RequiresConfiguredTargetTime()
        {
            var pass = Evaluate("c01_r02", new CareerEventOutcome(true, 0, finishTimeSeconds: 91.5d));
            Assert.That(pass.CompletedCount, Is.EqualTo(3));
            Assert.That(pass.AllCompleted, Is.True);
            Assert.That(pass.Entries[2].ObjectiveId, Is.EqualTo("time_c01_r02"));

            var miss = Evaluate("c01_r02", new CareerEventOutcome(true, 0, finishTimeSeconds: 92.1d));
            Assert.That(miss.CompletedCount, Is.EqualTo(2));
            Assert.That(miss.Entries[2].IsComplete, Is.False);
        }

        [Test]
        public void Elimination_RequiresFirstPlaceAndCleanTracksRestartsSeparately()
        {
            var result = Evaluate("c01_r03", new CareerEventOutcome(true, 1, finalPosition: 1));
            Assert.That(result.CompletedCount, Is.EqualTo(2));
            Assert.That(result.Entries[0].IsComplete, Is.True);
            Assert.That(result.Entries[1].IsComplete, Is.False);
            Assert.That(result.Entries[2].ObjectiveId, Is.EqualTo("win_c01_r03"));
            Assert.That(result.Entries[2].IsComplete, Is.True);
        }

        [Test]
        public void DriftChallenge_RequiresTargetDriftScore()
        {
            var pass = Evaluate("c01_r04", new CareerEventOutcome(true, 0, driftScore: 12000));
            Assert.That(pass.CompletedCount, Is.EqualTo(3));
            Assert.That(pass.Entries[2].ObjectiveId, Is.EqualTo("drift_c01_r04"));
            Assert.That(pass.Entries[2].IsComplete, Is.True);

            var miss = Evaluate("c01_r04", new CareerEventOutcome(true, 0, driftScore: 11999));
            Assert.That(miss.CompletedCount, Is.EqualTo(2));
            Assert.That(miss.AllCompleted, Is.False);
        }

        [Test]
        public void Boss_RequiresFirstPlace()
        {
            var win = Evaluate("c01_boss", new CareerEventOutcome(true, 0, finalPosition: 1));
            Assert.That(win.AllCompleted, Is.True);
            Assert.That(win.Entries[2].ObjectiveId, Is.EqualTo("win_c01_boss"));

            var loss = Evaluate("c01_boss", new CareerEventOutcome(true, 0, finalPosition: 2));
            Assert.That(loss.CompletedCount, Is.EqualTo(2));
            Assert.That(loss.AllCompleted, Is.False);
        }

        [Test]
        public void MissingModeMetrics_FailsObjectiveClosedWithoutThrowing()
        {
            Assert.That(Evaluate("c01_r02", new CareerEventOutcome(true, 0)).AllCompleted, Is.False);
            Assert.That(Evaluate("c01_r03", new CareerEventOutcome(true, 0)).AllCompleted, Is.False);
            Assert.That(Evaluate("c01_r04", new CareerEventOutcome(true, 0)).AllCompleted, Is.False);
        }

        [Test]
        public void UnfinishedEvent_CompletesNoObjectives()
        {
            var result = Evaluate("c01_r04", new CareerEventOutcome(false, 0, driftScore: 50000));
            Assert.That(result.CompletedCount, Is.Zero);
            Assert.That(result.AllCompleted, Is.False);
        }

        [Test]
        public void UnknownObjective_FailsClosed()
        {
            var source = FindDefinition("c01_r02");
            var invalid = new CareerNodeDefinition(
                source.Node,
                new[] { new CareerObjective("future_c01_r02", "Future rule", 1d) },
                source.Rewards);
            Assert.Throws<InvalidOperationException>(() =>
                CareerObjectiveEvaluationPolicy.Evaluate(invalid, new CareerEventOutcome(true, 0)));
        }

        [Test]
        public void InvalidOutcomeMetrics_FailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerEventOutcome(false, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerEventOutcome(true, 0, finishTimeSeconds: -1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerEventOutcome(true, 0, finalPosition: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerEventOutcome(true, 0, driftScore: -1));
        }

        private static CareerObjectiveEvaluation Evaluate(string nodeId, CareerEventOutcome outcome) =>
            CareerObjectiveEvaluationPolicy.Evaluate(FindDefinition(nodeId), outcome);

        private static CareerNodeDefinition FindDefinition(string nodeId)
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            for (var index = 0; index < definitions.Count; index++)
                if (StringComparer.Ordinal.Equals(definitions[index].Node.Id, nodeId))
                    return definitions[index];
            throw new KeyNotFoundException(nodeId);
        }
    }
}
