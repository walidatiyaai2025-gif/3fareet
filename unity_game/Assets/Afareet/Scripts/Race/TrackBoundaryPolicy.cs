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
            ValidatePath(orderedWaypoints);
            return SamplePrevalidated(orderedWaypoints, worldPosition, roadHalfWidth);
        }

        internal static TrackBoundarySample SamplePrevalidated(
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

                // delta is already flattened. Cross(up, normalize(delta)) equals
                // (delta.z, 0, -delta.x) / |delta|, so compute the unit right vector
                // directly instead of normalizing delta and then normalizing the cross.
                var inverseLength = 1f / Mathf.Sqrt(lengthSquared);
                var right = new Vector3(
                    delta.z * inverseLength,
                    0f,
                    -delta.x * inverseLength);
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

        internal static void ValidatePath(IReadOnlyList<Transform> orderedWaypoints)
        {
            if (orderedWaypoints == null)
                throw new ArgumentNullException(nameof(orderedWaypoints));
            if (orderedWaypoints.Count < 2)
                throw new ArgumentException("At least two ordered waypoints are required.", nameof(orderedWaypoints));

            for (var i = 0; i < orderedWaypoints.Count; i++)
            {
                if (orderedWaypoints[i] == null)
                    throw new ArgumentException("Ordered waypoints cannot contain null entries.", nameof(orderedWaypoints));
            }
        }

        private static Vector3 Flatten(Vector3 value) => new(value.x, 0f, value.z);
    }
}
