using System;
using Afareet.Progression;

namespace Afareet.CareerRuntime
{
    public interface ICareerRaceEventSource
    {
        event Action<float> ResultsReady;
        event Action RoundReset;
    }

    public interface ICareerRaceMetricsSource
    {
        int FinalPosition { get; }
        int DriftScore { get; }
    }

    public sealed class CareerRaceSessionCoordinator : IDisposable
    {
        private readonly ICareerRaceEventSource source;
        private readonly ICareerRaceMetricsSource metricsSource;
        private readonly CareerNodeDefinition definition;
        private CareerObjectiveEvaluation lastEvaluation;
        private int restartCount;
        private bool disposed;

        public int RestartCount => restartCount;
        public bool HasEvaluation => lastEvaluation != null;
        public CareerObjectiveEvaluation LastEvaluation => lastEvaluation;
        public bool IsDisposed => disposed;

        public event Action<CareerObjectiveEvaluation> EvaluationReady;

        public CareerRaceSessionCoordinator(
            ICareerRaceEventSource source,
            CareerNodeDefinition definition,
            ICareerRaceMetricsSource metricsSource = null)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.metricsSource = metricsSource;

            source.ResultsReady += OnResultsReady;
            source.RoundReset += OnRoundReset;
        }

        public void ResetSession()
        {
            ThrowIfDisposed();
            restartCount = 0;
            lastEvaluation = null;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            source.ResultsReady -= OnResultsReady;
            source.RoundReset -= OnRoundReset;
            lastEvaluation = null;
            EvaluationReady = null;
            disposed = true;
        }

        private void OnResultsReady(float finishTime)
        {
            if (disposed)
                return;

            var outcome = new CareerEventOutcome(
                finished: true,
                restartCount: restartCount,
                finishTimeSeconds: Math.Max(0d, finishTime),
                finalPosition: metricsSource?.FinalPosition,
                driftScore: metricsSource?.DriftScore ?? 0);
            var evaluation = CareerObjectiveEvaluationPolicy.Evaluate(definition, outcome);
            lastEvaluation = evaluation;
            EvaluationReady?.Invoke(evaluation);
        }

        private void OnRoundReset()
        {
            if (disposed)
                return;

            restartCount = checked(restartCount + 1);
            lastEvaluation = null;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CareerRaceSessionCoordinator));
        }
    }
}
