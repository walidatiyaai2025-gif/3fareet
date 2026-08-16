using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Afareet.Progression.Tests
{
    public sealed class CareerObjectiveEvaluationTests
    {
        [Test]
        public void FinishedEvent_CompletesFinishObjective()
        {
            var result = Evaluate("c01_r01", new CareerEventOutcome(true, 0));
            Assert.That(result.Entries.Count, Is.EqualTo(1));
            Assert.That(result.CompletedCount, Is.EqualTo(1));
            Assert.That(result.AllCompleted, Is.True);
        }

        [Test]
        public void CleanFinish_WithZeroRestarts_CompletesBothObjectives()
        {
            var result = Evaluate("c01_r02", new CareerEventOutcome(true, 0, finishTimeSeconds: 90d));
            Assert.That(result.Entries[0].IsComplete, Is.True);
            Assert.That(result.Entries[1].IsComplete, Is.True);
            Assert.That(result.Entries[2].IsComplete, Is.True);
            Assert.That(result.AllCompleted, Is.True);
        }

        [Test]
        public void RestartedFinish_BlocksCleanObjectiveOnly()
        {
            var result = Evaluate("c01_r02", new CareerEventOutcome(true, 1, finishTimeSeconds: 90d));
            Assert.That(result.CompletedCount, Is.EqualTo(2));
            Assert.That(result.Entries[0].IsComplete, Is.True);
            Assert.That(result.Entries[1].IsComplete, Is.False);
            Assert.That(result.Entries[2].IsComplete, Is.True);
        }

        [Test]
        public void TimeTrial_RequiresConfiguredTargetTime()
        {
            var pass = Evaluate("c01_r02", new CareerEventOutcome(true, 0, finishTimeSeconds: 91.5d));
            Assert.That(pass.AllCompleted, Is.True);
            var miss = Evaluate("c01_r02", new CareerEventOutcome(true, 0, finishTimeSeconds: 92.1d));
            Assert.That(miss.Entries[2].IsComplete, Is.False);
        }

        [Test]
        public void Elimination_RequiresFirstPlaceAndCleanTracksRestartsSeparately()
        {
            var result = Evaluate("c01_r03", new CareerEventOutcome(true, 1, finalPosition: 1));
            Assert.That(result.CompletedCount, Is.EqualTo(2));
            Assert.That(result.Entries[1].IsComplete, Is.False);
            Assert.That(result.Entries[2].ObjectiveId, Is.EqualTo("win_c01_r03"));
            Assert.That(result.Entries[2].IsComplete, Is.True);
        }

        [Test]
        public void DriftChallenge_RequiresTargetDriftScore()
        {
            var pass = Evaluate("c01_r04", new CareerEventOutcome(true, 0, driftScore: 12000));
            Assert.That(pass.AllCompleted, Is.True);
            var miss = Evaluate("c01_r04", new CareerEventOutcome(true, 0, driftScore: 11999));
            Assert.That(miss.AllCompleted, Is.False);
        }

        [Test]
        public void Boss_RequiresFirstPlace()
        {
            Assert.That(Evaluate("c01_boss", new CareerEventOutcome(true, 0, finalPosition: 1)).AllCompleted, Is.True);
            Assert.That(Evaluate("c01_boss", new CareerEventOutcome(true, 0, finalPosition: 2)).AllCompleted, Is.False);
        }

        [Test]
        public void UnfinishedEvent_CompletesNeitherObjective()
        {
            var result = Evaluate("c01_r04", new CareerEventOutcome(false, 0, driftScore: 50000));
            Assert.That(result.CompletedCount, Is.Zero);
            Assert.That(result.AllCompleted, Is.False);
        }

        [Test]
        public void Evaluation_PreservesDefinitionOrder()
        {
            var definition = FindDefinition("c01_boss");
            var result = CareerObjectiveEvaluationPolicy.Evaluate(definition, new CareerEventOutcome(true, 0, finalPosition: 1));
            for (var index = 0; index < definition.Objectives.Count; index++)
                Assert.That(result.Entries[index].ObjectiveId, Is.EqualTo(definition.Objectives[index].Id));
        }

        [Test]
        public void UnknownObjective_FailsClosed()
        {
            var source = FindDefinition("c01_r02");
            var invalid = new CareerNodeDefinition(source.Node, new[] { new CareerObjective("future_c01_r02", "Future rule", 1d) }, source.Rewards);
            Assert.Throws<InvalidOperationException>(() => CareerObjectiveEvaluationPolicy.Evaluate(invalid, new CareerEventOutcome(true, 0)));
        }

        [Test]
        public void NonBinaryTarget_FailsClosed()
        {
            var source = FindDefinition("c01_r02");
            var invalid = new CareerNodeDefinition(source.Node, new[] { new CareerObjective("finish_c01_r02", "Finish twice", 2d) }, source.Rewards);
            Assert.Throws<InvalidOperationException>(() => CareerObjectiveEvaluationPolicy.Evaluate(invalid, new CareerEventOutcome(true, 0)));
        }

        [Test]
        public void NegativeRestartCount_FailsClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CareerEventOutcome(false, -1));
        }

        [Test]
        public void InvalidModeMetrics_FailClosed()
        {
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
