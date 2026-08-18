using System.Collections.Generic;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Race
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class AiRacer : MonoBehaviour
    {
        private readonly RaycastHit[] avoidanceHits = new RaycastHit[6];
        private IReadOnlyList<Transform> waypoints;
        private ArcadeCarController car;
        private RacerCheckpointTracker checkpoints;
        private RivalResetController resetController;
        private RaceDirector powerUpDirector;
        private string powerUpRacerId;
        private int waypointIndex;
        private float skill;
        private float laneBias;
        private float aggression;
        private float overtakeSide;
        private AiDifficultyTuning difficultyTuning = AiDifficultyTuning.Standard;

        public string PowerUpRacerId => powerUpRacerId;
        public bool HasPowerUpRuntimeBinding => powerUpDirector != null && !string.IsNullOrWhiteSpace(powerUpRacerId);
        public AiDifficultyTuning DifficultyTuning => difficultyTuning;
        public int NavigationWaypointIndex => waypointIndex;

        public void Configure(IReadOnlyList<Transform> path, int rivalIndex)
        {
            if (path == null) throw new System.ArgumentNullException(nameof(path));
            if (path.Count < 3) throw new System.ArgumentException("At least three waypoints are required.", nameof(path));
            for (var index = 0; index < path.Count; index++)
                if (path[index] == null) throw new System.ArgumentException($"Waypoint {index} is null.", nameof(path));

            // The race path is immutable after runtime composition. Validate the entire path
            // once here so FixedUpdate can use the prevalidated racing-line hot path instead
            // of rescanning every waypoint for every rival on every physics tick.
            waypoints = path;
            var random = new System.Random(17011 + rivalIndex * 7919);
            aggression = Mathf.Lerp(.68f, .96f, (float)random.NextDouble());
            laneBias = Mathf.Lerp(-1.15f, 1.15f, (float)random.NextDouble());
            overtakeSide = random.Next(0, 2) == 0 ? -1f : 1f;
            skill = Mathf.Clamp(.78f + rivalIndex * .07f + aggression * .08f, .80f, .98f);
            SynchronizeNavigation(path.Count - rivalIndex * 2);
        }

        public void SynchronizeNavigation(int nextWaypointIndex)
        {
            if (waypoints == null || waypoints.Count == 0)
                return;

            waypointIndex = Wrap(nextWaypointIndex, waypoints.Count);
        }

        public void ApplyDifficultyTuning(AiDifficultyTuning tuning)
        {
            // AiDifficultyTuning validates its range at construction. Keeping this as a separate
            // multiplier preserves the deterministic seeded base profile and makes Standard
            // (1.0 / 1.0) exactly equivalent to the pre-Career behavior.
            difficultyTuning = tuning;
        }

        public void BindPowerUpRuntime(RaceDirector director, string racerId)
        {
            if (director == null) throw new System.ArgumentNullException(nameof(director));
            powerUpDirector = director;
            powerUpRacerId = PowerUpRaceRuntime.ValidateRacerId(racerId, nameof(racerId));
        }

        internal AiPowerUpExecutionResult EvaluateBoundPowerUpDecision()
        {
            if (!HasPowerUpRuntimeBinding || !isActiveAndEnabled) return null;
            return powerUpDirector.ExecuteBoundAiPowerUp(powerUpRacerId);
        }

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        private void OnEnable()
        {
            BindRaceProgressRuntime();
            SynchronizeWithCheckpointProgress();
        }

        private void OnDisable()
        {
            UnbindResetController();
        }

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Count < 3) return;

            var effectiveSkill = Mathf.Clamp(skill * difficultyTuning.PaceMultiplier, .55f, 1.25f);
            var effectiveAggression = Mathf.Clamp(aggression * difficultyTuning.AggressionMultiplier, .50f, 1.30f);
            var speedKph = Mathf.Abs(car.SpeedKph);
            var racingPlan = RacingLineLookahead.PlanPrevalidated(waypoints, waypointIndex, speedKph);
            var target = waypoints[racingPlan.AimWaypointIndex];
            var targetPosition = target.position + target.right * laneBias;
            var local = transform.InverseTransformPoint(targetPosition);
            var steer = Mathf.Clamp(local.x / Mathf.Max(2f, local.magnitude * .28f), -1f, 1f);

            var avoidance = ComputeAvoidance(effectiveAggression, out var carAhead, out var nearestCarDistance);
            steer = Mathf.Clamp(steer + avoidance, -1f, 1f);

            var brakeStrength = racingPlan.SpeedPlan.Brake01;
            var throttle = Mathf.Lerp(effectiveSkill, .12f, brakeStrength);
            if (carAhead && nearestCarDistance < 3.5f)
            {
                throttle *= .68f;
                brakeStrength = Mathf.Max(brakeStrength, .22f);
            }
            else if (carAhead)
            {
                throttle = Mathf.Min(1f, throttle + effectiveAggression * .08f);
            }

            var corner = Mathf.Max(racingPlan.SpeedPlan.Severity01, Mathf.Abs(steer));
            var drift = corner > .48f && speedKph > 65f;
            var nitro = racingPlan.UseNitro &&
                        Mathf.Abs(steer) < .22f &&
                        car.NitroEnergy > .35f &&
                        (!carAhead || nearestCarDistance > 4.5f);
            var brake = brakeStrength > .08f;
            car.SetAiInput(throttle, steer, drift, nitro, brake);

            var checkpointLocal = transform.InverseTransformPoint(waypoints[waypointIndex].position);
            if (checkpointLocal.magnitude < 9f)
                waypointIndex = (waypointIndex + 1) % waypoints.Count;
        }

        private void BindRaceProgressRuntime()
        {
            if (checkpoints == null)
                checkpoints = GetComponent<RacerCheckpointTracker>();

            var currentResetController = GetComponent<RivalResetController>();
            if (currentResetController == resetController)
                return;

            UnbindResetController();
            resetController = currentResetController;
            if (resetController != null)
                resetController.RivalReset += OnRivalReset;
        }

        private void SynchronizeWithCheckpointProgress()
        {
            if (checkpoints == null || !checkpoints.IsConfigured)
                return;

            SynchronizeNavigation(checkpoints.ExpectedCheckpointIndex);
        }

        private void OnRivalReset(int resetWaypointIndex)
        {
            if (checkpoints != null && checkpoints.IsConfigured)
            {
                SynchronizeWithCheckpointProgress();
                return;
            }

            SynchronizeNavigation(resetWaypointIndex + 1);
        }

        private void UnbindResetController()
        {
            if (resetController != null)
                resetController.RivalReset -= OnRivalReset;
            resetController = null;
        }

        private float ComputeAvoidance(float effectiveAggression, out bool carAhead, out float nearestCarDistance)
        {
            carAhead = false;
            nearestCarDistance = float.MaxValue;
            var count = Physics.SphereCastNonAlloc(
                transform.position + Vector3.up * .55f,
                .78f,
                transform.forward,
                avoidanceHits,
                8f,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            var avoidance = 0f;
            for (var i = 0; i < count; i++)
            {
                var hit = avoidanceHits[i];
                if (hit.collider == null || hit.collider.transform.root == transform.root) continue;
                var otherCar = hit.collider.transform.root.GetComponent<ArcadeCarController>();
                if (otherCar == null) continue;

                carAhead = true;
                nearestCarDistance = Mathf.Min(nearestCarDistance, hit.distance);
                var localHit = transform.InverseTransformPoint(hit.point);
                var side = Mathf.Abs(localHit.x) < .2f ? overtakeSide : -Mathf.Sign(localHit.x);
                var proximity = 1f - Mathf.Clamp01(hit.distance / 8f);
                avoidance += side * proximity * Mathf.Lerp(.34f, .68f, effectiveAggression);
            }

            return Mathf.Clamp(avoidance, -.7f, .7f);
        }

        private static int Wrap(int index, int count)
        {
            var value = index % count;
            return value < 0 ? value + count : value;
        }
    }
}