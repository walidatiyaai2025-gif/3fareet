using System;
using Afareet.Progression;
using Afareet.Race;

namespace Afareet.CareerRuntime
{
    public sealed class RaceRoundCareerSessionAdapter : IDisposable
    {
        private sealed class RaceRoundEventSource : ICareerRaceEventSource
        {
            private readonly RaceRoundController round;

            public RaceRoundEventSource(RaceRoundController round)
            {
                this.round = round ?? throw new ArgumentNullException(nameof(round));
            }

            public event Action<float> ResultsReady
            {
                add => round.ResultsReady += value;
                remove => round.ResultsReady -= value;
            }

            public event Action RoundReset
            {
                add => round.RoundReset += value;
                remove => round.RoundReset -= value;
            }
        }

        private readonly CareerRaceSessionCoordinator coordinator;

        public int RestartCount => coordinator.RestartCount;
        public bool HasEvaluation => coordinator.HasEvaluation;
        public CareerObjectiveEvaluation LastEvaluation => coordinator.LastEvaluation;
        public bool IsDisposed => coordinator.IsDisposed;

        public event Action<CareerObjectiveEvaluation> EvaluationReady
        {
            add => coordinator.EvaluationReady += value;
            remove => coordinator.EvaluationReady -= value;
        }

        public RaceRoundCareerSessionAdapter(
            RaceRoundController round,
            CareerNodeDefinition definition)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            coordinator = new CareerRaceSessionCoordinator(
                new RaceRoundEventSource(round),
                definition);
        }

        public void ResetSession()
        {
            coordinator.ResetSession();
        }

        public void Dispose()
        {
            coordinator.Dispose();
        }
    }
}
