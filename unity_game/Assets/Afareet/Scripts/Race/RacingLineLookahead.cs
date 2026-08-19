using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.Race
{
    public readonly struct RacingLinePlan
    {
        public RacingLinePlan(int aimIndex, CornerSpeedPlan speedPlan, bool useNitro)
        {
            AimWaypointIndex = aimIndex;
            SpeedPlan = speedPlan;
            UseNitro = useNitro;
        }
        public int AimWaypointIndex { get; }
        public CornerSpeedPlan SpeedPlan { get; }
        public bool UseNitro { get; }
    }

    public static class RacingLineLookahead
    {
        public static RacingLinePlan Plan(
            IReadOnlyList<Transform> waypoints,
            int targetIndex,
            float speedKph,
            int lookAhead = 3)
        {
            ValidatePath(waypoints);
            return PlanPrevalidated(waypoints, targetIndex, speedKph, lookAhead);
        }

        internal static RacingLinePlan PlanPrevalidated(
            IReadOnlyList<Transform> waypoints,
            int targetIndex,
            float speedKph,
            int lookAhead = 3)
        {
            if (waypoints == null) throw new ArgumentNullException(nameof(waypoints));
            if (waypoints.Count < 3) throw new ArgumentException("At least three waypoints are required.", nameof(waypoints));
            if (targetIndex < 0 || targetIndex >= waypoints.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
            if (lookAhead < 1) throw new ArgumentOutOfRangeException(nameof(lookAhead));

            var severity = 0f;
            var inspected = Mathf.Min(lookAhead, waypoints.Count);
            for (var offset = 0; offset < inspected; offset++)
            {
                var index = Wrap(targetIndex + offset, waypoints.Count);
                var previous = waypoints[Wrap(index - 1, waypoints.Count)].position;
                var current = waypoints[index].position;
                var next = waypoints[Wrap(index + 1, waypoints.Count)].position;
                var weighted = CornerSpeedPolicy.Severity(previous, current, next) / (1f + offset * .45f);
                severity = Mathf.Max(severity, weighted);
            }

            var speedPlan = CornerSpeedPolicy.Plan(severity, speedKph);
            var aimOffset = severity < .2f ? 2 : severity < .55f ? 1 : 0;
            var aimIndex = Wrap(targetIndex + aimOffset, waypoints.Count);
            var nitro = severity < .18f && speedPlan.Brake01 < .05f;
            return new RacingLinePlan(aimIndex, speedPlan, nitro);
        }

        internal static void ValidatePath(IReadOnlyList<Transform> waypoints)
        {
            if (waypoints == null) throw new ArgumentNullException(nameof(waypoints));
            if (waypoints.Count < 3) throw new ArgumentException("At least three waypoints are required.", nameof(waypoints));
            for (var i = 0; i < waypoints.Count; i++)
                if (waypoints[i] == null) throw new ArgumentException($"Waypoint {i} is null.", nameof(waypoints));
        }

        private static int Wrap(int index, int count)
        {
            var value = index % count;
            return value < 0 ? value + count : value;
        }
    }
}
