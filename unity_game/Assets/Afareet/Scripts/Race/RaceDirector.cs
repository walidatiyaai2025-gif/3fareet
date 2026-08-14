using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Race
{
    public sealed class RaceDirector : MonoBehaviour
    {
        private const float P1RoadHalfWidth = 7f;

        private sealed class RacerRuntime
        {
            public ArcadeCarController Car;
            public RacerCheckpointTracker Checkpoints;
            public OneLapRaceTracker Lap;
            public int StableOrder;
            public string RacerId;
        }

        private readonly List<ArcadeCarController> registeredRivals = new();
        private readonly List<RacerRuntime> racers = new();
        private ArcadeCarController player;
        private TrackRuntime track;
        private RaceRoundController round;
        private Transform checkpointRoot;
        private Transform boundaryRoot;
        private bool racersReleased;

        public RaceRoundPhase Phase => round?.Phase ?? RaceRoundPhase.Ready;
        public float RaceTime => PlayerRuntime?.Lap.ElapsedTime ?? 0f;
        public float FinishTime => round?.FinishTime ?? -1f;
        public bool IsStarted => Phase != RaceRoundPhase.Ready;
        public bool IsPaused { get; private set; }
        public string CountdownText
        {
            get
            {
                if (Phase != RaceRoundPhase.Countdown) return string.Empty;
                var remaining = round.CountdownRemaining;
                return remaining > 1f ? Mathf.CeilToInt(remaining).ToString() : remaining > 0f ? "GO!" : string.Empty;
            }
        }

        public int Position => CalculatePosition(player);

        public event Action<bool> PauseChanged;
        public event Action<float> ResultsReady;

        private RacerRuntime PlayerRuntime => racers.Count > 0 ? racers[0] : null;

        public void Configure(ArcadeCarController playerCar, TrackRuntime runtimeTrack)
        {
            if (playerCar == null) throw new ArgumentNullException(nameof(playerCar));
            if (runtimeTrack == null) throw new ArgumentNullException(nameof(runtimeTrack));
            if (runtimeTrack.Waypoints.Count < 2) throw new ArgumentException("Track requires at least two ordered waypoints.", nameof(runtimeTrack));

            UnsubscribeRound();
            player = playerCar;
            track = runtimeTrack;
            racers.Clear();

            PrepareRacer(player, "PLAYER", 0);
            for (var i = 0; i < registeredRivals.Count; i++)
                PrepareRacer(registeredRivals[i], $"RIVAL-{i + 1:00}", i + 1);

            round = player.GetComponent<RaceRoundController>();
            if (round == null) round = player.gameObject.AddComponent<RaceRoundController>();
            round.Configure(track.Waypoints.Count, 3f);
            round.RaceStarted += OnRoundRaceStarted;
            round.ResultsReady += OnRoundResultsReady;
            round.RoundReset += OnRoundReset;

            BuildCheckpointVolumes();
            BuildTrackBoundaries();
            SetPausedInternal(false);
            FreezeRacers();
            ResetRacersToGrid();
        }

        public void RegisterRival(ArcadeCarController rival)
        {
            if (rival == null) throw new ArgumentNullException(nameof(rival));
            if (registeredRivals.Contains(rival)) return;

            registeredRivals.Add(rival);
            if (track != null)
            {
                PrepareRacer(rival, $"RIVAL-{registeredRivals.Count:00}", registeredRivals.Count);
                FreezeRacer(rival);
            }
            else
            {
                FreezeRacer(rival);
            }
        }

        public void StartRace()
        {
            if (round == null) throw new InvalidOperationException("RaceDirector must be configured before starting.");
            if (Phase != RaceRoundPhase.Ready) return;

            racersReleased = false;
            SetPausedInternal(false);
            FreezeRacers();
            round.BeginCountdown();
        }

        public bool SetPaused(bool paused)
        {
            if (paused && Phase != RaceRoundPhase.Racing) return false;
            if (!paused && !IsPaused) return true;
            SetPausedInternal(paused);
            return true;
        }

        public bool RestartRace()
        {
            if (round == null || Phase != RaceRoundPhase.Results) return false;

            SetPausedInternal(false);
            round.RestartRound();
            for (var i = 1; i < racers.Count; i++)
                racers[i].Lap.Configure(track.Waypoints.Count);

            racersReleased = false;
            FreezeRacers();
            ResetRacersToGrid();
            StartRace();
            return true;
        }

        private void PrepareRacer(ArcadeCarController car, string racerId, int stableOrder)
        {
            if (car == null) return;
            for (var i = 0; i < racers.Count; i++)
                if (racers[i].Car == car) return;

            var checkpoints = car.GetComponent<RacerCheckpointTracker>();
            if (checkpoints == null) checkpoints = car.gameObject.AddComponent<RacerCheckpointTracker>();

            var lap = car.GetComponent<OneLapRaceTracker>();
            if (lap == null) lap = car.gameObject.AddComponent<OneLapRaceTracker>();
            lap.Configure(track.Waypoints.Count);

            TrackBoundaryRuntimeBuilder.EnsureMonitor(car.gameObject, track, P1RoadHalfWidth);

            if (stableOrder > 0)
            {
                var reset = car.GetComponent<RivalResetController>();
                if (reset == null) reset = car.gameObject.AddComponent<RivalResetController>();
                reset.Configure(track.Waypoints, checkpoints);
                reset.SetActive(false);
            }

            racers.Add(new RacerRuntime
            {
                Car = car,
                Checkpoints = checkpoints,
                Lap = lap,
                StableOrder = stableOrder,
                RacerId = racerId
            });
        }

        private void BuildCheckpointVolumes()
        {
            if (checkpointRoot != null) Destroy(checkpointRoot.gameObject);
            checkpointRoot = new GameObject("ORDERED RACE CHECKPOINTS").transform;
            checkpointRoot.SetParent(transform, false);
            RaceCheckpointRuntimeBuilder.Build(track, checkpointRoot);
        }

        private void BuildTrackBoundaries()
        {
            if (boundaryRoot != null) Destroy(boundaryRoot.gameObject);
            boundaryRoot = new GameObject("TRACK BOUNDARY EDGES").transform;
            boundaryRoot.SetParent(transform, false);
            TrackBoundaryRuntimeBuilder.BuildEdges(track, boundaryRoot, P1RoadHalfWidth);
        }

        private void OnRoundRaceStarted()
        {
            for (var i = 1; i < racers.Count; i++)
                if (!racers[i].Lap.IsStarted) racers[i].Lap.StartRace();
            ReleaseRacers();
        }

        private void OnRoundResultsReady(float finishTime)
        {
            SetPausedInternal(false);
            FreezeRacers();
            ResultsReady?.Invoke(finishTime);
        }

        private void OnRoundReset()
        {
            racersReleased = false;
            SetRivalRecoveryActive(false);
        }

        private void ReleaseRacers()
        {
            if (racersReleased) return;
            racersReleased = true;

            for (var i = 0; i < racers.Count; i++)
            {
                var car = racers[i].Car;
                var body = car.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = false;
                if (car == player)
                {
                    car.AcceptsPlayerInput = true;
                }
                else
                {
                    var ai = car.GetComponent<AiRacer>();
                    if (ai != null) ai.enabled = true;
                }
            }

            SetRivalRecoveryActive(true);
        }

        private void FreezeRacers()
        {
            for (var i = 0; i < racers.Count; i++) FreezeRacer(racers[i].Car);
            SetRivalRecoveryActive(false);
        }

        private void FreezeRacer(ArcadeCarController car)
        {
            if (car == null) return;
            car.AcceptsPlayerInput = false;
            car.SetAiInput(0f, 0f, false, false, false);
            var body = car.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            var ai = car.GetComponent<AiRacer>();
            if (ai != null) ai.enabled = false;
        }

        private void SetRivalRecoveryActive(bool active)
        {
            for (var i = 1; i < racers.Count; i++)
            {
                var reset = racers[i].Car.GetComponent<RivalResetController>();
                if (reset != null) reset.SetActive(active);
            }
        }

        private void ResetRacersToGrid()
        {
            if (track == null || track.Waypoints.Count == 0) return;
            var rotation = track.Waypoints[0].rotation;
            for (var i = 0; i < racers.Count; i++)
            {
                var runtime = racers[i];
                runtime.Car.transform.SetPositionAndRotation(track.GridPosition(runtime.StableOrder), rotation);
                var body = runtime.Car.GetComponent<Rigidbody>();
                if (body == null) continue;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private int CalculatePosition(ArcadeCarController target)
        {
            if (target == null || track == null || racers.Count == 0) return 1;

            var snapshots = new List<RaceProgressSnapshot>(racers.Count);
            for (var i = 0; i < racers.Count; i++)
            {
                var runtime = racers[i];
                snapshots.Add(RaceRanking.Capture(
                    runtime.RacerId,
                    runtime.Checkpoints,
                    runtime.Lap,
                    SegmentProgress(runtime),
                    runtime.StableOrder));
            }

            var ranked = RaceRanking.Rank(snapshots);
            var targetRuntime = FindRuntime(target);
            if (targetRuntime == null) return 1;
            for (var i = 0; i < ranked.Count; i++)
                if (ranked[i].Progress.RacerId == targetRuntime.RacerId) return ranked[i].Position;
            return 1;
        }

        private float SegmentProgress(RacerRuntime runtime)
        {
            var expected = runtime.Checkpoints.ExpectedCheckpointIndex;
            if (expected < 0 || track == null || track.Waypoints.Count < 2) return 0f;

            var count = track.Waypoints.Count;
            var previous = (expected - 1 + count) % count;
            var a = track.Waypoints[previous].position;
            var b = track.Waypoints[expected].position;
            var segment = b - a;
            var denominator = segment.sqrMagnitude;
            if (denominator <= 0.0001f) return 0f;
            return Mathf.Clamp01(Vector3.Dot(runtime.Car.transform.position - a, segment) / denominator);
        }

        private RacerRuntime FindRuntime(ArcadeCarController car)
        {
            for (var i = 0; i < racers.Count; i++)
                if (racers[i].Car == car) return racers[i];
            return null;
        }

        private void SetPausedInternal(bool paused)
        {
            if (IsPaused == paused && Mathf.Approximately(Time.timeScale, paused ? 0f : 1f)) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(paused);
        }

        private void UnsubscribeRound()
        {
            if (round == null) return;
            round.RaceStarted -= OnRoundRaceStarted;
            round.ResultsReady -= OnRoundResultsReady;
            round.RoundReset -= OnRoundReset;
        }

        private void OnDestroy()
        {
            UnsubscribeRound();
            if (IsPaused) Time.timeScale = 1f;
        }
    }
}
