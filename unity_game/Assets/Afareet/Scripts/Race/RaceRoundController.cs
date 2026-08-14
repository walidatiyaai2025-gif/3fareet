using System;
using UnityEngine;
namespace Afareet.Race
{
    [RequireComponent(typeof(OneLapRaceTracker))]
    public sealed class RaceRoundController : MonoBehaviour
    {
        private OneLapRaceTracker lapTracker;
        private RaceRoundFlowState flow;
        private int checkpointCount;
        private float countdownSeconds;
        public RaceRoundPhase Phase => flow?.Phase ?? RaceRoundPhase.Ready;
        public float CountdownRemaining => flow?.CountdownRemaining ?? 0f;
        public float FinishTime => flow?.FinishTime ?? -1f;
        public int RoundNumber => flow?.RoundNumber ?? 0;
        public event Action CountdownStarted;
        public event Action RaceStarted;
        public event Action<float> ResultsReady;
        public event Action RoundReset;

        public void Configure(int orderedCheckpointCount, float countdownDurationSeconds = 3f)
        {
            if (orderedCheckpointCount < 2) throw new ArgumentOutOfRangeException(nameof(orderedCheckpointCount));
            if (countdownDurationSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(countdownDurationSeconds));
            if (lapTracker != null) lapTracker.RaceFinished -= OnRaceFinished;
            checkpointCount = orderedCheckpointCount;
            countdownSeconds = countdownDurationSeconds;
            lapTracker = GetComponent<OneLapRaceTracker>();
            lapTracker.Configure(checkpointCount);
            lapTracker.RaceFinished += OnRaceFinished;
            flow = new RaceRoundFlowState();
        }

        public void BeginCountdown()
        {
            EnsureConfigured();
            flow.BeginCountdown(countdownSeconds);
            CountdownStarted?.Invoke();
        }

        public bool AdvanceCountdown(float deltaTime)
        {
            EnsureConfigured();
            if (!flow.TickCountdown(deltaTime)) return false;
            lapTracker.StartRace();
            RaceStarted?.Invoke();
            return true;
        }

        public void RestartRound()
        {
            EnsureConfigured();
            flow.Restart();
            lapTracker.Configure(checkpointCount);
            RoundReset?.Invoke();
        }

        private void Update()
        {
            if (flow != null && flow.Phase == RaceRoundPhase.Countdown) AdvanceCountdown(Time.deltaTime);
        }

        private void OnRaceFinished(float finishTime)
        {
            if (flow == null || flow.Phase != RaceRoundPhase.Racing) return;
            flow.Finish(finishTime);
            ResultsReady?.Invoke(finishTime);
        }

        private void OnDestroy()
        {
            if (lapTracker != null) lapTracker.RaceFinished -= OnRaceFinished;
        }

        private void EnsureConfigured()
        {
            if (flow == null || lapTracker == null) throw new InvalidOperationException();
        }
    }
}
