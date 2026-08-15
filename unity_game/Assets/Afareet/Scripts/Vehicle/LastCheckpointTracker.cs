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
        private float nextSample;

        public bool HasCheckpoint => lastCheckpoint != null;
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

        private void Update()
        {
            if (waypoints.Count == 0) DiscoverWaypoints();
            if (waypoints.Count == 0 || Time.time < nextSample) return;
            nextSample = Time.time + .25f;
            if (Vector3.Dot(transform.up, Vector3.up) < .55f) return;

            Transform nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var waypoint in waypoints)
            {
                if (Vector3.Dot(transform.forward, waypoint.forward) < MinimumForwardAlignment)
                    continue;

                var distance = (waypoint.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = waypoint;
                nearestDistance = distance;
            }

            if (nearest != null && nearestDistance <= MaximumCaptureDistanceSqr)
                lastCheckpoint = nearest;
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
