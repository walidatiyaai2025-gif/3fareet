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
        private int waypointIndex;
        private float skill;
        private float laneBias;
        private float aggression;
        private float overtakeSide;

        public void Configure(IReadOnlyList<Transform> path, int rivalIndex)
        {
            waypoints = path;
            var random = new System.Random(17011 + rivalIndex * 7919);
            aggression = Mathf.Lerp(.68f, .96f, (float)random.NextDouble());
            laneBias = Mathf.Lerp(-1.15f, 1.15f, (float)random.NextDouble());
            overtakeSide = random.Next(0, 2) == 0 ? -1f : 1f;
            skill = Mathf.Clamp(.78f + rivalIndex * .07f + aggression * .08f, .80f, .98f);
            waypointIndex = (path.Count - rivalIndex * 2) % path.Count;
        }

        private void Awake() => car = GetComponent<ArcadeCarController>();

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Count < 3) return;

            var racingPlan = RacingLineLookahead.Plan(waypoints, waypointIndex, Mathf.Abs(car.SpeedKph));
            var target = waypoints[racingPlan.AimWaypointIndex];
            var targetPosition = target.position + target.right * laneBias;
            var local = transform.InverseTransformPoint(targetPosition);
            var steer = Mathf.Clamp(local.x / Mathf.Max(2f, local.magnitude * .28f), -1f, 1f);

            var avoidance = ComputeAvoidance(out var carAhead, out var nearestCarDistance);
            steer = Mathf.Clamp(steer + avoidance, -1f, 1f);

            var brakeStrength = racingPlan.SpeedPlan.Brake01;
            var throttle = Mathf.Lerp(skill, .12f, brakeStrength);
            if (carAhead && nearestCarDistance < 3.5f)
            {
                throttle *= .68f;
                brakeStrength = Mathf.Max(brakeStrength, .22f);
            }
            else if (carAhead)
            {
                throttle = Mathf.Min(1f, throttle + aggression * .08f);
            }

            var corner = Mathf.Max(racingPlan.SpeedPlan.Severity01, Mathf.Abs(steer));
            var drift = corner > .48f && car.SpeedKph > 65f;
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

        private float ComputeAvoidance(out bool carAhead, out float nearestCarDistance)
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
                avoidance += side * proximity * Mathf.Lerp(.34f, .68f, aggression);
            }

            return Mathf.Clamp(avoidance, -.7f, .7f);
        }
    }
}
