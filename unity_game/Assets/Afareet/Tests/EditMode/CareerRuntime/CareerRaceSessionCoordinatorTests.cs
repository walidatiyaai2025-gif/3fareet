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

            public void EmitResults(float finishTime = 90f)
            {
                ResultsReady?.Invoke(finishTime);
            }

            public void EmitReset()
            {
                RoundReset?.Invoke();
            }
        }

        [Test]
        public void InitialFinish_CompletesFinishAndCleanObjectives()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, DefinitionWithCleanObjective());

            source.EmitResults();

            Assert.That(coordinator.RestartCount, Is.EqualTo(0));
            Assert.That(coordinator.HasEvaluation, Is.True);
            Assert.That(coordinator.LastEvaluation.CompletedCount, Is.EqualTo(2));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.True);

            coordinator.Dispose();
        }

        [Test]
        public void RoundReset_InvalidatesPriorEvaluation_AndNextFinishIsNotClean()
        {
            var source = new FakeRaceEventSource();
            var definition = DefinitionWithCleanObjective();
            var coordinator = new CareerRaceSessionCoordinator(source, definition);

            source.EmitResults();
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.True);

            source.EmitReset();

            Assert.That(coordinator.RestartCount, Is.EqualTo(1));
            Assert.That(coordinator.HasEvaluation, Is.False);
            Assert.That(coordinator.LastEvaluation, Is.Null);

            source.EmitResults();

            Assert.That(coordinator.LastEvaluation.CompletedCount, Is.EqualTo(1));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.False);
            Assert.That(coordinator.LastEvaluation.Entries[0].ObjectiveId, Is.EqualTo($"finish_{definition.Node.Id}"));
            Assert.That(coordinator.LastEvaluation.Entries[0].IsComplete, Is.True);
            Assert.That(coordinator.LastEvaluation.Entries[1].ObjectiveId, Is.EqualTo($"clean_{definition.Node.Id}"));
            Assert.That(coordinator.LastEvaluation.Entries[1].IsComplete, Is.False);

            coordinator.Dispose();
        }

        [Test]
        public void ResetSession_StartsFreshZeroRestartSession()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, DefinitionWithCleanObjective());

            source.EmitReset();
            source.EmitReset();
            source.EmitResults();
            Assert.That(coordinator.RestartCount, Is.EqualTo(2));
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.False);

            coordinator.ResetSession();

            Assert.That(coordinator.RestartCount, Is.EqualTo(0));
            Assert.That(coordinator.HasEvaluation, Is.False);
            source.EmitResults();
            Assert.That(coordinator.LastEvaluation.AllCompleted, Is.True);

            coordinator.Dispose();
        }

        [Test]
        public void EvaluationReady_FiresOncePerResultsEvent()
        {
            var source = new FakeRaceEventSource();
            var coordinator = new CareerRaceSessionCoordinator(source, DefinitionWithCleanObjective());
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
            var coordinator = new CareerRaceSessionCoordinator(source, DefinitionWithCleanObjective());

            source.EmitResults();
            Assert.That(coordinator.HasEvaluation, Is.True);

            coordinator.Dispose();
            coordinator.Dispose();
            source.EmitReset();
            source.EmitResults();

            Assert.That(coordinator.IsDisposed, Is.True);
            Assert.That(coordinator.RestartCount, Is.EqualTo(0));
            Assert.That(coordinator.HasEvaluation, Is.False);
            Assert.That(coordinator.LastEvaluation, Is.Null);
            Assert.Throws<ObjectDisposedException>(() => coordinator.ResetSession());
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            var source = new FakeRaceEventSource();
            var definition = DefinitionWithCleanObjective();

            Assert.Throws<ArgumentNullException>(() => new CareerRaceSessionCoordinator(null, definition));
            Assert.Throws<ArgumentNullException>(() => new CareerRaceSessionCoordinator(source, null));
        }

        private static CareerNodeDefinition DefinitionWithCleanObjective()
        {
            return ChapterOneCareerEventContent.CreateDefinitions()[1];
        }
    }
}
