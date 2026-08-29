using System;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Race
{
    public enum OneLapRacePhase
    {
        Ready,
        Racing,
        Finished
    }

    public sealed class OneLapRaceState
    {
        public int CheckpointCount { get; }
        public OneLapRacePhase Phase { get; private set; } = OneLapRacePhase.Ready;
        public int AcceptedCheckpointsThisLap { get; private set; }
        public int CompletedLaps { get; private set; }
        public float ElapsedTime { get; private set; }
        public float FinishTime { get; private set; } = -1f;
        public bool IsStarted => Phase != OneLapRacePhase.Ready;
        public bool IsFinished => Phase == OneLapRacePhase.Finished;

        public OneLapRaceState(int checkpointCount)
        {
            if (checkpointCount < 2)
                throw new ArgumentOutOfRangeException(nameof(checkpointCount));
            CheckpointCount = checkpointCount;
        }

        public void StartRace()
        {
            if (Phase != OneLapRacePhase.Ready)
                throw new InvalidOperationException("The one-lap race has already started.");
            Phase = OneLapRacePhase.Racing;
            AcceptedCheckpointsThisLap = 0;
            CompletedLaps = 0;
            ElapsedTime = 0f;
            FinishTime = -1f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Phase == OneLapRacePhase.Racing)
                ElapsedTime += deltaTime;
        }

        public bool NotifyAcceptedCheckpoint(int checkpointIndex)
        {
            if (checkpointIndex < 0 || checkpointIndex >= CheckpointCount)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));
            if (Phase != OneLapRacePhase.Racing)
                return false;

            AcceptedCheckpointsThisLap++;
            if (checkpointIndex != 0 || AcceptedCheckpointsThisLap != CheckpointCount)
                return false;

            CompletedLaps = 1;
            FinishTime = ElapsedTime;
            Phase = OneLapRacePhase.Finished;
            return true;
        }
    }

    [RequireComponent(typeof(RacerCheckpointTracker))]
    public sealed class OneLapRaceTracker : MonoBehaviour
    {
        private RacerCheckpointTracker checkpointTracker;
        private ArcadeCarController car;
        private OneLapRaceState state;

        public bool IsConfigured => state != null;
        public bool IsStarted => state?.IsStarted ?? false;
        public bool IsFinished => state?.IsFinished ?? false;
        public OneLapRacePhase Phase => state?.Phase ?? OneLapRacePhase.Ready;
        public int CompletedLaps => state?.CompletedLaps ?? 0;
        public float ElapsedTime => state?.ElapsedTime ?? 0f;
        public float FinishTime => state?.FinishTime ?? -1f;

        public event Action RaceStarted;
        public event Action<float> RaceFinished;

        public void Configure(int checkpointCount)
        {
            if (checkpointCount < 2)
                throw new ArgumentOutOfRangeException(nameof(checkpointCount));

            if (checkpointTracker != null)
                checkpointTracker.CheckpointAccepted -= OnCheckpointAccepted;

            checkpointTracker = GetComponent<RacerCheckpointTracker>();
            checkpointTracker.Configure(checkpointCount, firstExpectedCheckpointIndex: 1);
            checkpointTracker.CheckpointAccepted += OnCheckpointAccepted;
            car = GetComponent<ArcadeCarController>();
            state = new OneLapRaceState(checkpointCount);
        }

        public void StartRace()
        {
            EnsureConfigured();
            checkpointTracker.ResetProgress(firstExpectedCheckpointIndex: 1);

            // Every lap start is a fresh race attempt. Vehicle tuning/configuration persists,
            // but consumable, control, drift and recovery-lock state must never leak from the
            // previous Results -> Restart cycle into the new green flag.
            car?.ResetRaceTransientState();

            state.StartRace();
            RaceStarted?.Invoke();
        }

        public void AdvanceTime(float deltaTime)
        {
            EnsureConfigured();
            state.Tick(deltaTime);
        }

        private void Update()
        {
            if (state != null && state.Phase == OneLapRacePhase.Racing)
                state.Tick(Time.deltaTime);
        }

        private void OnCheckpointAccepted(int checkpointIndex)
        {
            if (state == null) return;
            if (state.NotifyAcceptedCheckpoint(checkpointIndex))
                RaceFinished?.Invoke(state.FinishTime);
        }

        private void OnDestroy()
        {
            if (checkpointTracker != null)
                checkpointTracker.CheckpointAccepted -= OnCheckpointAccepted;
        }

        private void EnsureConfigured()
        {
            if (state == null || checkpointTracker == null)
                throw new InvalidOperationException("One-lap race tracker must be configured before use.");
        }
    }
}
