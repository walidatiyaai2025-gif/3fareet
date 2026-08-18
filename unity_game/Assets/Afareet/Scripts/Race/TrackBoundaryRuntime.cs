using System;
using System.Collections.Generic;
using Afareet.World;
using UnityEngine;

namespace Afareet.Race
{
    public sealed class TrackBoundaryMonitor : MonoBehaviour
    {
        private TrackRuntime track;
        private float roadHalfWidth;
        private bool hasSample;

        public TrackBoundarySample LastSample { get; private set; }
        public bool IsOffRoad => hasSample && !LastSample.IsOnRoad;
        public event Action<bool> OffRoadStateChanged;

        public void Configure(TrackRuntime runtimeTrack, float halfWidth)
        {
            if (runtimeTrack == null) throw new ArgumentNullException(nameof(runtimeTrack));
            if (halfWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(halfWidth));

            // Runtime track geometry is immutable after composition. Validate the complete
            // waypoint contract once here so FixedUpdate can sample the configured corridor
            // without repeating a full-path null scan for every racer on every physics tick.
            TrackBoundaryPolicy.ValidatePath(runtimeTrack.Waypoints);
            track = runtimeTrack;
            roadHalfWidth = halfWidth;
            hasSample = false;
            Refresh();
        }

        public TrackBoundarySample Refresh()
        {
            if (track == null) throw new InvalidOperationException("TrackBoundaryMonitor must be configured before use.");
            var wasOffRoad = IsOffRoad;
            LastSample = TrackBoundaryPolicy.SamplePrevalidated(track.Waypoints, transform.position, roadHalfWidth);
            hasSample = true;
            var isOffRoad = IsOffRoad;
            if (wasOffRoad != isOffRoad) OffRoadStateChanged?.Invoke(isOffRoad);
            return LastSample;
        }

        private void FixedUpdate()
        {
            if (track != null) Refresh();
        }
    }

    public static class TrackBoundaryRuntimeBuilder
    {
        public static IReadOnlyList<BoxCollider> BuildEdges(TrackRuntime track, Transform parent, float roadHalfWidth = 7f, float edgeHeight = 1.4f, float edgeThickness = .45f)
        {
            if (track == null) throw new ArgumentNullException(nameof(track));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (track.Waypoints.Count < 2) throw new ArgumentException("Track requires at least two waypoints.", nameof(track));
            if (roadHalfWidth <= 0f || edgeHeight <= 0f || edgeThickness <= 0f) throw new ArgumentOutOfRangeException();

            var result = new List<BoxCollider>(track.Waypoints.Count * 2);
            for (var i = 0; i < track.Waypoints.Count; i++)
            {
                var a = track.Waypoints[i];
                var b = track.Waypoints[(i + 1) % track.Waypoints.Count];
                if (a == null || b == null) throw new ArgumentException("Waypoints cannot be null.", nameof(track));

                var delta = b.position - a.position;
                delta.y = 0f;
                var length = delta.magnitude;
                if (length <= .01f) continue;

                var forward = delta / length;
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                var rotation = Quaternion.LookRotation(forward, Vector3.up);
                var center = (a.position + b.position) * .5f + Vector3.up * (edgeHeight * .5f);
                var offset = roadHalfWidth + edgeThickness * .5f;
                result.Add(CreateEdge(parent, $"Track Edge L {i:00}", center - right * offset, rotation, length, edgeHeight, edgeThickness));
                result.Add(CreateEdge(parent, $"Track Edge R {i:00}", center + right * offset, rotation, length, edgeHeight, edgeThickness));
            }
            return result;
        }

        public static TrackBoundaryMonitor EnsureMonitor(GameObject racer, TrackRuntime track, float roadHalfWidth = 7f)
        {
            if (racer == null) throw new ArgumentNullException(nameof(racer));
            var monitor = racer.GetComponent<TrackBoundaryMonitor>();
            if (monitor == null) monitor = racer.AddComponent<TrackBoundaryMonitor>();
            monitor.Configure(track, roadHalfWidth);
            return monitor;
        }

        private static BoxCollider CreateEdge(Transform parent, string name, Vector3 position, Quaternion rotation, float length, float height, float thickness)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            var collider = obj.AddComponent<BoxCollider>();
            collider.size = new Vector3(thickness, height, length + .25f);
            collider.isTrigger = false;
            return collider;
        }
    }
}