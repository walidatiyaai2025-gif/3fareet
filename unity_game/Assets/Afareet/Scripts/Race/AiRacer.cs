using System.Collections.Generic;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Race
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class AiRacer : MonoBehaviour
    {
        private IReadOnlyList<Transform> waypoints;
        private ArcadeCarController car;
        private int waypointIndex;
        private float skill;

        public void Configure(IReadOnlyList<Transform> path, int rivalIndex)
        {
            waypoints = path;
            skill = .82f + rivalIndex * .08f;
            waypointIndex = (path.Count - rivalIndex * 2) % path.Count;
        }

        private void Awake() => car = GetComponent<ArcadeCarController>();

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Count == 0) return;
            var target = waypoints[waypointIndex];
            var local = transform.InverseTransformPoint(target.position);
            var steer = Mathf.Clamp(local.x / Mathf.Max(2f, local.magnitude * .28f), -1f, 1f);
            var corner = Mathf.Abs(steer);
            var throttle = corner > .72f ? .48f : skill;
            var drift = corner > .48f && car.SpeedKph > 65f;
            var nitro = corner < .15f && car.NitroEnergy > .35f;
            car.SetAiInput(throttle, steer, drift, nitro);
            if (local.magnitude < 9f) waypointIndex = (waypointIndex + 1) % waypoints.Count;
        }
    }
}
