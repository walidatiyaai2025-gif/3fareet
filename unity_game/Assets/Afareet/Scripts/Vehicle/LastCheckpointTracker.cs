using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class LastCheckpointTracker : MonoBehaviour
    {
        private const float MaximumCaptureDistanceSqr = 196f;
        private const float MinimumForwardAlignment = 0.15f;

        private readonly List<Transform> waypoints = new();
        private Transform lastCheckpoint;
        private int lastCheckpointIndex = -1;
        private int startupFallbackBaselineIndex = -1;
        private float nextSample;
        private bool hasValidatedRaceCheckpointFeed;

        public bool HasCheckpoint => lastCheckpoint != null;
        public int LastCheckpointIndex => lastCheckpointIndex;
        public int StartupFallbackBaselineIndex => startupFallbackBaselineIndex;
        public bool HasValidatedRaceCheckpointFeed => hasValidatedRaceCheckpointFeed;
        public Vector3 Position => lastCheckpoint == null ? transform.position : lastCheckpoint.position;
        public Quaternion Rotation => lastCheckpoint == null ? transform.rotation : lastCheckpoint.rotation;
        public Vector3 RecoveryPosition => lastCheckpoint == null
            ? transform.position + Vector3.up * VehicleRecoveryPolicy.RecoveryUpOffsetMeters
            : VehicleRecoveryPolicy.SafeRecoveryPosition(lastCheckpoint.position, lastCheckpoint.rotation);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<Installer>() != null) return;
            var host = new GameObject("AFAREET LAST CHECKPOINT INSTALLER");
            DontDestroyOnLoad(host);
            host.AddComponent<Installer>();
        }

        public void AcceptValidatedRaceCheckpoint(int checkpointIndex, Transform checkpoint)
        {
            if (checkpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(checkpointIndex));
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));

            lastCheckpoint = checkpoint;
            lastCheckpointIndex = checkpointIndex;
            hasValidatedRaceCheckpointFeed = true;
        }

        public void ResetValidatedRaceProgress(int firstExpectedCheckpointIndex = 0)
        {
            if (firstExpectedCheckpointIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(firstExpectedCheckpointIndex));

            lastCheckpoint = null;
            lastCheckpointIndex = -1;
            startupFallbackBaselineIndex = firstExpectedCheckpointIndex;
            hasValidatedRaceCheckpointFeed = false;
            nextSample = 0f;
        }

        private void Update()
        {
            // Once the ordered Race checkpoint system has supplied a checkpoint, it is the
            // authoritative recovery source. Nearest-waypoint sampling remains only a startup
            // fallback before the first accepted checkpoint (and after a race restart reset).
            if (hasValidatedRaceCheckpointFeed) return;

            if (waypoints.Count == 0) DiscoverWaypoints();
            if (waypoints.Count == 0 || Time.time < nextSample) return;
            nextSample = Time.time + .25f;
            if (Vector3.Dot(transform.up, Vector3.up) < .55f) return;

            // Do not sample at all until the race system supplies its ordered baseline. More
            // importantly, keep that baseline fixed before the first accepted checkpoint: a
            // series of spatially-near samples must not creep four points at a time through a
            // folded/chicane section and eventually jump to an unrelated sector.
            if (startupFallbackBaselineIndex < 0 || startupFallbackBaselineIndex >= waypoints.Count)
                return;

            Transform nearest = null;
            var nearestIndex = -1;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < waypoints.Count; index++)
            {
                if (!VehicleRecoveryPolicy.IsRecoveryCheckpointAdvanceAllowed(
                        startupFallbackBaselineIndex,
                        index,
                        waypoints.Count))
                    continue;

                var waypoint = waypoints[index];
                if (Vector3.Dot(transform.forward, waypoint.forward) < MinimumForwardAlignment)
                    continue;

                var distance = (waypoint.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = waypoint;
                nearestIndex = index;
                nearestDistance = distance;
            }

            if (nearest != null && nearestDistance <= MaximumCaptureDistanceSqr)
            {
                lastCheckpoint = nearest;
                lastCheckpointIndex = nearestIndex;
            }
        }

        private void DiscoverWaypoints()
        {
            for (var i = 0; i < 256; i++)
            {
                var waypoint = GameObject.Find($"Waypoint {i:00}");
                if (waypoint == null)
                {
                    if (i > 0) break;
                    continue;
                }
                waypoints.Add(waypoint.transform);
            }
        }

        private sealed class Installer : MonoBehaviour
        {
            private void Update()
            {
                var hero = GameObject.Find("PLAYER HERO — AFAREET");
                if (hero == null) return;
                if (hero.GetComponent<LastCheckpointTracker>() == null)
                    hero.AddComponent<LastCheckpointTracker>();
                Destroy(gameObject);
            }
        }
    }
}
