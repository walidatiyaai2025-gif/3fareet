using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.Race
{
    public readonly struct TrackBoundarySample
    {
        public TrackBoundarySample(
            int segmentIndex,
            float segmentProgress01,
            float signedLateralDistance,
            float roadHalfWidth,
            Vector3 closestPoint)
        {
            SegmentIndex = segmentIndex;
            SegmentProgress01 = segmentProgress01;
            SignedLateralDistance = signedLateralDistance;
            RoadHalfWidth = roadHalfWidth;
            ClosestPoint = closestPoint;
        }

        public int SegmentIndex { get; }
        public float SegmentProgress01 { get; }
        public float SignedLateralDistance { get; }
        public float LateralDistance => Mathf.Abs(SignedLateralDistance);
        public float RoadHalfWidth { get; }
        public Vector3 ClosestPoint { get; }
        public bool IsOnRoad => LateralDistance <= RoadHalfWidth;
    }

    /// <summary>
    /// Pure corridor sampling for URAC-006. It deliberately uses the validated
    /// ordered track polyline rather than physics overlap state so off-road
    /// classification remains deterministic and testable.
    /// </summary>
    public static class TrackBoundaryPolicy
    {
        public static TrackBoundarySample Sample(
            IReadOnlyList<Transform> orderedWaypoints,
            Vector3 worldPosition,
            float roadHalfWidth)
        {
            if (orderedWaypoints == null)
                throw new ArgumentNullException(nameof(orderedWaypoints));
            if (orderedWaypoints.Count < 2)
                throw new ArgumentException("At least two ordered waypoints are required.", nameof(orderedWaypoints));
            if (roadHalfWidth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(roadHalfWidth));

            var position = Flatten(worldPosition);
            var bestDistanceSquared = float.PositiveInfinity;
            var bestSegment = -1;
            var bestProgress = 0f;
            var bestSignedLateral = 0f;
            var bestClosestPoint = Vector3.zero;

            for (var i = 0; i < orderedWaypoints.Count; i++)
            {
                var current = orderedWaypoints[i];
                var next = orderedWaypoints[(i + 1) % orderedWaypoints.Count];
                if (current == null || next == null)
                    throw new ArgumentException("Ordered waypoints cannot contain null entries.", nameof(orderedWaypoints));

                var start = Flatten(current.position);
                var end = Flatten(next.position);
                var delta = end - start;
                var lengthSquared = delta.sqrMagnitude;
                if (lengthSquared <= .0001f) continue;

                var progress = Mathf.Clamp01(Vector3.Dot(position - start, delta) / lengthSquared);
                var closest = start + delta * progress;
                var offset = position - closest;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared) continue;

                var forward = delta.normalized;
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                bestDistanceSquared = distanceSquared;
                bestSegment = i;
                bestProgress = progress;
                bestSignedLateral = Vector3.Dot(offset, right);
                bestClosestPoint = new Vector3(closest.x, worldPosition.y, closest.z);
            }

            if (bestSegment < 0)
                throw new ArgumentException("Track contains no non-degenerate segments.", nameof(orderedWaypoints));

            return new TrackBoundarySample(
                bestSegment,
                bestProgress,
                bestSignedLateral,
                roadHalfWidth,
                bestClosestPoint);
        }

        private static Vector3 Flatten(Vector3 value) => new(value.x, 0f, value.z);
    }
}
