using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Race
{
    [RequireComponent(typeof(ArcadeCarController), typeof(Rigidbody))]
    public sealed class RivalResetController : MonoBehaviour
    {
        private ArcadeCarController car;
        private Rigidbody body;
        private IReadOnlyList<Transform> waypoints;
        private RacerCheckpointTracker checkpoints;
        private RivalMotionGuard motionGuard;

        public bool Active { get; private set; }
        public int ResetCount { get; private set; }
        public int LastResetWaypointIndex { get; private set; } = -1;
        public event Action<int> RivalReset;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            body = GetComponent<Rigidbody>();
        }

        public void Configure(IReadOnlyList<Transform> orderedWaypoints, RacerCheckpointTracker checkpointTracker, float lowSpeedKph = 4f, float delaySeconds = 2.5f)
        {
            if (orderedWaypoints == null) throw new ArgumentNullException(nameof(orderedWaypoints));
            if (orderedWaypoints.Count < 2) throw new ArgumentException("At least two waypoints are required.", nameof(orderedWaypoints));
            for (var i = 0; i < orderedWaypoints.Count; i++) if (orderedWaypoints[i] == null) throw new ArgumentException($"Waypoint {i} is null.", nameof(orderedWaypoints));
            if (checkpointTracker == null || !checkpointTracker.IsConfigured) throw new ArgumentException("A configured checkpoint tracker is required.", nameof(checkpointTracker));
            waypoints = orderedWaypoints;
            checkpoints = checkpointTracker;
            motionGuard = new RivalMotionGuard(lowSpeedKph, delaySeconds);
            Active = false;
            ResetCount = 0;
            LastResetWaypointIndex = -1;
        }

        public void SetActive(bool active)
        {
            if (motionGuard == null) throw new InvalidOperationException("Configure the reset controller first.");
            Active = active;
            motionGuard.Reset();
        }

        public bool Evaluate(float absoluteSpeedKph, float deltaSeconds)
        {
            if (!Active) return false;
            if (!motionGuard.Observe(absoluteSpeedKph, deltaSeconds)) return false;
            ResetToLastAcceptedWaypoint();
            return true;
        }

        private void FixedUpdate()
        {
            if (Active) Evaluate(Mathf.Abs(car.SpeedKph), Time.fixedDeltaTime);
        }

        private void ResetToLastAcceptedWaypoint()
        {
            var index = Wrap(checkpoints.ExpectedCheckpointIndex - 1, waypoints.Count);
            var target = waypoints[index];
            transform.SetPositionAndRotation(target.position + Vector3.up * .75f, target.rotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            car.SetAiInput(0f, 0f, false, false);
            LastResetWaypointIndex = index;
            ResetCount++;
            RivalReset?.Invoke(index);
        }

        private static int Wrap(int index, int count)
        {
            var value = index % count;
            return value < 0 ? value + count : value;
        }
    }
}
