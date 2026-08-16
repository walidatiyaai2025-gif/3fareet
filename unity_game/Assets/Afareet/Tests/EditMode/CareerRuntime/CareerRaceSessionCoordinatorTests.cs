using System;
using Afareet.CareerRuntime;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.CareerRuntime
{
    public sealed class CareerRaceSessionCoordinatorTests
    {
        private sealed class FakeRaceEventSource : ICareerRaceEventSource
        {
            public event Action<float> ResultsReady;
            public event Action RoundReset;
            public void EmitResults(float finishTime = 90f) => ResultsReady?.Invoke(finishTime);
            public void EmitReset() => RoundReset?.Invoke();
        }

        private sealed class FakeMetricsSource : ICareerRaceMetricsSource
        {
            public int FinalPosition { get; set; } = 1;
            public int DriftScore { get; set; }
        }

        [Test]
        public void TimeTrialFinish_UsesFinishTimeAndCompletesModeObjective()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r02"));

            source.EmitResults(90f);

            Assert.That(coordinator.RestartCount, Is.Zero);
            Assert.That(coordinator.LastEvaluation.CompletedCount, Is.EqualTo(3));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.True);
            coordinator.Dispose();
        }

        [Test]
        public void RoundReset_InvalidatesPriorEvaluation_AndBlocksCleanOnly()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r02"));

            source.EmitResults(90f);
            source.EmitReset();
            Assert.That(coordinator.RestartCount, Is.EqualTo(1));
            Assert.That(coordinator.HasEvaluation, Is.False);

            source.EmitResults(90f);

            Assert.That(coordinator.LastEvaluation.CompletedCount, Is.EqualTo(2));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.False);
            Assert.That(coordinator.LastEvaluation.Entries[0].IsComplete, Is.True);
            Assert.That(coordinator.LastEvaluation.Entries[1].IsComplete, Is.False);
            Assert.That(coordinator.LastEvaluation.Entries[2].IsComplete, Is.True);
            coordinator.Dispose();
        }

        [Test]
        public void DriftAndPositionMetrics_AreForwardedToEvaluation()
        {
            var source = new FakeRaceEventSource();
            var metrics = new FakeMetricsSource { FinalPosition = 1, DriftScore = 12000 };
            var drift = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r04"), metrics);

            source.EmitResults(70f);
            Assert.That(drift.LastEvaluation.AllCompleted, Is.True);
            drift.Dispose();

            var boss = new CareerRaceSessionCoordinator(source, FindDefinition("c01_boss"), metrics);
            source.EmitResults(70f);
            Assert.That(boss.LastEvaluation.AllCompleted, Is.True);
            boss.Dispose();
        }

        [Test]
        public void MissingPositionMetric_FailsWinObjectiveClosed()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r03"));
            source.EmitResults();
            Assert.That(coordinator.LastEvaluation.CompletedCount, Is.EqualTo(2));
            Assert.That(coordinator.LastEvaluation.Entries[2].IsComplete, Is.False);
            coordinator.Dispose();
        }

        [Test]
        public void ResetSession_StartsFreshZeroRestartSession()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r02"));
            source.EmitReset();
            source.EmitReset();
            source.EmitResults(90f);
            Assert.That(coordinator.RestartCount, Is.EqualTo(2));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.False);

            coordinator.ResetSession();
            Assert.That(coordinator.RestartCount, Is.Zero);
            Assert.That(coordinator.HasEvaluation, Is.False);
            source.EmitResults(90f);
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.True);
            coordinator.Dispose();
        }

        [Test]
        public void EvaluationReady_FiresOncePerResultsEvent()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r02"));
            var callbacks = 0;
            coordinator.EvaluationReady += _ => callbacks++;
            source.EmitResults();
            source.EmitReset();
            source.EmitResults();
            Assert.That(callbacks, Is.EqualTo(2));
            coordinator.Dispose();
        }

        [Test]
        public void Dispose_ClearsStaleEvaluation_UnsubscribesAndIsIdempotent()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, FindDefinition("c01_r02"));
            source.EmitResults();
            Assert.That(coordinator.HasEvaluation, Is.True);
            coordinator.Dispose();
            coordinator.Dispose();
            source.EmitReset();
            source.EmitResults();
            Assert.That(coordinator.IsDisposed, Is.True);
            Assert.That(coordinator.RestartCount, Is.Zero);
            Assert.That(coordinator.HasEvaluation, Is.False);
            Assert.Throws<ObjectDisposedException>(() => coordinator.ResetSession());
        }

        [Test]
        public void Constructor_RejectsMissingRequiredDependencies()
        {
            var source = new FakeRaceEventSource();
            var definition = FindDefinition("c01_r02");
            Assert.Throws<ArgumentNullException>(() => new CareerRaceSessionCoordinator(null, definition));
            Assert.Throws<ArgumentNullException>(() => new CareerRaceSessionCoordinator(source, null));
        }

        private static CareerNodeDefinition FindDefinition(string nodeId)
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            for (var index = 0; index < definitions.Count; index++)
                if (definitions[index].Node.Id == nodeId)
                    return definitions[index];
            throw new InvalidOperationException(nodeId);
        }
    }
}
