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
            var definition = FindDefinition("c01_r01");

            var result = CareerObjectiveEvaluationPolicy.Evaluate(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0));

            Assert.That(result.Entries.Count, Is.EqualTo(1));
            Assert.That(result.Entries[0].ObjectiveId, Is.EqualTo("finish_c01_r01"));
            Assert.That(result.Entries[0].Value, Is.EqualTo(1d));
            Assert.That(result.Entries[0].IsComplete, Is.True);
            Assert.That(result.AllCompleted, Is.True);
        }

        [Test]
        public void CleanFinish_WithZeroRestarts_CompletesBothObjectives()
        {
            var definition = FindDefinition("c01_r02");

            var result = CareerObjectiveEvaluationPolicy.Evaluate(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0));

            Assert.That(result.Entries.Count, Is.EqualTo(2));
            Assert.That(result.CompletedCount, Is.EqualTo(2));
            Assert.That(result.AllCompleted, Is.True);
            Assert.That(result.Entries[0].ObjectiveId, Is.EqualTo("finish_c01_r02"));
            Assert.That(result.Entries[1].ObjectiveId, Is.EqualTo("clean_c01_r02"));
        }

        [Test]
        public void RestartedFinish_BlocksCleanObjectiveOnly()
        {
            var definition = FindDefinition("c01_r03");

            var result = CareerObjectiveEvaluationPolicy.Evaluate(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 1));

            Assert.That(result.CompletedCount, Is.EqualTo(1));
            Assert.That(result.AllCompleted, Is.False);
            Assert.That(result.Entries[0].IsComplete, Is.True);
            Assert.That(result.Entries[1].IsComplete, Is.False);
            Assert.That(result.Entries[1].Value, Is.EqualTo(0d));
        }

        [Test]
        public void UnfinishedEvent_CompletesNeitherObjective()
        {
            var definition = FindDefinition("c01_r04");

            var result = CareerObjectiveEvaluationPolicy.Evaluate(
                definition,
                new CareerEventOutcome(finished: false, restartCount: 0));

            Assert.That(result.CompletedCount, Is.Zero);
            Assert.That(result.AllCompleted, Is.False);
            Assert.That(result.Entries[0].Value, Is.EqualTo(0d));
            Assert.That(result.Entries[1].Value, Is.EqualTo(0d));
        }

        [Test]
        public void Evaluation_PreservesDefinitionOrder()
        {
            var definition = FindDefinition("c01_boss");

            var result = CareerObjectiveEvaluationPolicy.Evaluate(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0));

            Assert.That(result.Entries[0].ObjectiveId, Is.EqualTo(definition.Objectives[0].Id));
            Assert.That(result.Entries[1].ObjectiveId, Is.EqualTo(definition.Objectives[1].Id));
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
                CareerObjectiveEvaluationPolicy.Evaluate(
                    invalid,
                    new CareerEventOutcome(finished: true, restartCount: 0)));
        }

        [Test]
        public void NonBinaryTarget_FailsClosed()
        {
            var source = FindDefinition("c01_r02");
            var invalid = new CareerNodeDefinition(
                source.Node,
                new[] { new CareerObjective("finish_c01_r02", "Finish twice", 2d) },
                source.Rewards);

            Assert.Throws<InvalidOperationException>(() =>
                CareerObjectiveEvaluationPolicy.Evaluate(
                    invalid,
                    new CareerEventOutcome(finished: true, restartCount: 0)));
        }

        [Test]
        public void NegativeRestartCount_FailsClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CareerEventOutcome(finished: false, restartCount: -1));
        }

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
