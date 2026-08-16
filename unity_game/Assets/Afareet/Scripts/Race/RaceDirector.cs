using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Race
{
    [DefaultExecutionOrder(-200)]
    public sealed class RaceDirector : MonoBehaviour
    {
        private const float P1RoadHalfWidth = 7f;
        private const double AiPowerUpDecisionCadenceSeconds = .5d;

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
        private PowerUpRaceRuntime powerUpRuntime;
        private Transform checkpointRoot;
        private Transform boundaryRoot;
        private bool racersReleased;
        private bool powerUpRuntimeDirty = true;
        private double nextPowerUpDecisionRaceTime;

        public RaceRoundPhase Phase => round?.Phase ?? RaceRoundPhase.Ready;
        public float RaceTime => PlayerRuntime?.Lap.ElapsedTime ?? 0f;
        public float FinishTime => round?.FinishTime ?? -1f;
        public bool IsStarted => Phase != RaceRoundPhase.Ready;
        public bool IsPaused { get; private set; }
        public bool HasPowerUpRuntime => powerUpRuntime != null && !powerUpRuntimeDirty;
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
            powerUpRuntime = null;
            powerUpRuntimeDirty = true;
            nextPowerUpDecisionRaceTime = 0d;

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

            EnsurePowerUpRuntime();
            ResetPowerUpDriveModifiers();
            nextPowerUpDecisionRaceTime = 0d;
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

        internal AiPowerUpExecutionResult ExecuteBoundAiPowerUp(string racerId)
        {
            if (Phase != RaceRoundPhase.Racing || IsPaused || powerUpRuntime == null || powerUpRuntimeDirty)
                return null;

            var source = FindRuntime(racerId);
            if (source == null || source.Lap.IsFinished)
                return null;

            var ranked = BuildRankedRace();
            var rankedIndex = -1;
            for (var i = 0; i < ranked.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(ranked[i].Progress.RacerId, source.RacerId))
                {
                    rankedIndex = i;
                    break;
                }
            }

            if (rankedIndex < 0)
                return null;

            var targetAhead = rankedIndex > 0
                ? FindRuntime(ranked[rankedIndex - 1].Progress.RacerId)
                : null;
            var chaserBehind = rankedIndex + 1 < ranked.Count
                ? FindRuntime(ranked[rankedIndex + 1].Progress.RacerId)
                : null;

            var elapsedRaceSeconds = Math.Max(0d, source.Lap.ElapsedTime);
            var snapshot = AiPowerUpLiveSnapshotBuilder.Build(
                position: rankedIndex + 1,
                fieldSize: ranked.Count,
                acceptedCheckpoints: source.Checkpoints.AcceptedCount,
                checkpointCount: track.Waypoints.Count,
                segmentProgress: SegmentProgress(source),
                ownSpeedKph: Math.Abs(source.Car.SpeedKph),
                hasTargetAhead: targetAhead != null,
                targetDistanceMeters: targetAhead == null
                    ? 0d
                    : Vector3.Distance(source.Car.transform.position, targetAhead.Car.transform.position),
                targetSpeedKph: targetAhead == null ? 0d : Math.Abs(targetAhead.Car.SpeedKph),
                hasChaserBehind: chaserBehind != null,
                chaserDistanceMeters: chaserBehind == null
                    ? 0d
                    : Vector3.Distance(source.Car.transform.position, chaserBehind.Car.transform.position),
                incomingHostilePressure: false,
                elapsedRaceSeconds: elapsedRaceSeconds);

            return powerUpRuntime.ExecuteAiDecision(
                source.RacerId,
                snapshot,
                targetAhead?.RacerId,
                chaserBehind?.RacerId,
                elapsedRaceSeconds);
        }

        private void FixedUpdate()
        {
            if (Phase != RaceRoundPhase.Racing || IsPaused || powerUpRuntime == null || powerUpRuntimeDirty)
                return;

            var raceTimeSeconds = Math.Max(0d, RaceTime);
            var tickResults = powerUpRuntime.TickAll(raceTimeSeconds);
            var driveProjectionDirty = false;
            for (var i = 0; i < tickResults.Count; i++)
            {
                if (tickResults[i].ExpiredEffectCount > 0)
                {
                    driveProjectionDirty = true;
                    break;
                }
            }

            if (raceTimeSeconds + .0001d >= nextPowerUpDecisionRaceTime)
            {
                nextPowerUpDecisionRaceTime = raceTimeSeconds + AiPowerUpDecisionCadenceSeconds;
                for (var i = 1; i < racers.Count; i++)
                {
                    var ai = racers[i].Car.GetComponent<AiRacer>();
                    if (ai == null) continue;

                    var execution = ai.EvaluateBoundPowerUpDecision();
                    if (execution?.UseResult != null && execution.UseResult.Status == PowerUpRuntimeUseStatus.Used)
                        driveProjectionDirty = true;
                }
            }

            if (driveProjectionDirty)
                ApplyPowerUpDriveModifiers(raceTimeSeconds);
        }

        private void PrepareRacer(ArcadeCarController car, string racerId, int stableOrder)
        {
            if (car == null) return;
            for (var i = 0; i < racers.Count; i++)
                if (racers[i].Car == car) return;

            car.ResetExternalDriveModifier();

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
            powerUpRuntimeDirty = true;
            if (powerUpRuntime != null)
                ResetPowerUpDriveModifiers();
        }

        private void EnsurePowerUpRuntime()
        {
            if (powerUpRuntime != null && !powerUpRuntimeDirty)
                return;

            var registrations = new List<PowerUpRacerRegistration>(racers.Count);
            for (var i = 0; i < racers.Count; i++)
                registrations.Add(new PowerUpRacerRegistration(racers[i].RacerId));

            powerUpRuntime = new PowerUpRaceRuntime(
                PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
                registrations);
            powerUpRuntimeDirty = false;
            nextPowerUpDecisionRaceTime = 0d;
            ResetPowerUpDriveModifiers();

            for (var i = 1; i < racers.Count; i++)
            {
                var ai = racers[i].Car.GetComponent<AiRacer>();
                if (ai != null)
                    ai.BindPowerUpRuntime(this, racers[i].RacerId);
            }
        }

        private void ApplyPowerUpDriveModifiers(double raceTimeSeconds)
        {
            for (var i = 0; i < racers.Count; i++)
            {
                var runtime = racers[i];
                var projection = powerUpRuntime.GetVehicleEffectProjection(runtime.RacerId, raceTimeSeconds);
                runtime.Car.SetExternalDriveModifier(new ArcadeDriveModifier(
                    projection.AccelerationMultiplier,
                    projection.MaxSpeedMultiplier,
                    projection.SteeringAuthorityMultiplier,
                    projection.GripMultiplier));
            }
        }

        private void ResetPowerUpDriveModifiers()
        {
            for (var i = 0; i < racers.Count; i++)
                racers[i].Car.ResetExternalDriveModifier();
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
            ResetPowerUpDriveModifiers();
            FreezeRacers();
            ResultsReady?.Invoke(finishTime);
        }

        private void OnRoundReset()
        {
            racersReleased = false;
            nextPowerUpDecisionRaceTime = 0d;
            if (powerUpRuntime != null)
                powerUpRuntime.ResetRace();
            ResetPowerUpDriveModifiers();
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
            car.ResetExternalDriveModifier();
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

        private IReadOnlyList<RankedRaceEntry> BuildRankedRace()
        {
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

            return RaceRanking.Rank(snapshots);
        }

        private int CalculatePosition(ArcadeCarController target)
        {
            if (target == null || track == null || racers.Count == 0) return 1;

            var ranked = BuildRankedRace();
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

        private RacerRuntime FindRuntime(string racerId)
        {
            if (string.IsNullOrWhiteSpace(racerId)) return null;
            for (var i = 0; i < racers.Count; i++)
                if (StringComparer.Ordinal.Equals(racers[i].RacerId, racerId)) return racers[i];
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
            ResetPowerUpDriveModifiers();
            UnsubscribeRound();
            if (IsPaused) Time.timeScale = 1f;
        }
    }
}
