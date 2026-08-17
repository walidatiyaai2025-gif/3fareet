using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Corrects the authored UART-005 rail placement contract at runtime.
    /// SM_Prop_CairoBarrier_A is authored 2m long on local X (-1..+1). TrackBuilder
    /// supplies a segment-start position and scales local X to half the desired segment
    /// length, so long race rails are first centered on their segment. Once all race rails
    /// are present, each side is trimmed/extended to the real intersection of the adjacent
    /// offset lines. This is important on authored sharp corners: the outside rail must
    /// extend while the inside rail must shorten, otherwise identical unsigned miter
    /// extensions make barriers cross diagonally into the driving lane.
    /// </summary>
    public sealed class CairoAuthoredRailCenteringPass : MonoBehaviour
    {
        private const string TrackRootName = "CAIRO NIGHT RUN // 3FAREET";
        private const string BarrierName = "AUTHORED CAIRO BARRIER";
        private const float AuthoredHalfLengthMeters = 1f;
        private const float MinimumRailScaleX = 1.25f;
        private const int ExpectedWaypoints = 72;
        private const int ExpectedRaceRails = ExpectedWaypoints * 2;
        private const float RoadWidth = 14f;
        private const float RailLateralOffset = RoadWidth * .56f;
        private const float MaxJoinShiftMeters = RailLateralOffset * 1.5f;
        private const float MinimumDirectionAlignment = .92f;

        private readonly HashSet<Transform> corrected = new();
        private float nextScanAt;
        private bool completionLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CairoAuthoredRailCenteringPass>() != null) return;
            var host = new GameObject("AFAREET UART005 AUTHORED RAIL CENTERING PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoAuthoredRailCenteringPass>();
        }

        private void Awake()
        {
            nextScanAt = Time.unscaledTime + .2f;
        }

        private void Update()
        {
            if (completionLogged || Time.unscaledTime < nextScanAt) return;
            nextScanAt = Time.unscaledTime + .25f;

            foreach (var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!IsRaceRail(candidate) || corrected.Contains(candidate)) continue;

                var halfLength = Mathf.Abs(candidate.localScale.x) * AuthoredHalfLengthMeters;
                candidate.position += candidate.right * halfLength;
                corrected.Add(candidate);
            }

            if (corrected.Count < ExpectedRaceRails) return;

            var signedJoinAdjusted = ApplySignedJoinCorrection();
            if (signedJoinAdjusted < ExpectedRaceRails) return;

            completionLogged = true;
            Debug.Log(
                $"AFAREET_UART005_AUTHORED_RAIL_CENTERING_ACTIVE corrected={corrected.Count} " +
                $"expected={ExpectedRaceRails} sourceHalfLength={AuthoredHalfLengthMeters:F1}m " +
                $"signedJoinAdjusted={signedJoinAdjusted} placement=signed-offset-line-intersections " +
                "primitiveGeometry=false");
            Debug.Log(
                $"AFAREET_URAC011_SIGNED_RAIL_JOINS_ACTIVE adjusted={signedJoinAdjusted} " +
                $"railOffset={RailLateralOffset:F2}m maxJoinShift={MaxJoinShiftMeters:F2}m " +
                "insideShortens=true outsideExtends=true raceLineUnchanged=true collisionsUnchanged=true");
        }

        private int ApplySignedJoinCorrection()
        {
            var rootObject = GameObject.Find(TrackRootName);
            if (rootObject == null) return 0;
            var root = rootObject.transform;

            var waypoints = new Transform[ExpectedWaypoints];
            for (var index = 0; index < waypoints.Length; index++)
            {
                waypoints[index] = root.Find($"Waypoint {index:00}");
                if (waypoints[index] == null) return 0;
            }

            var available = new List<Transform>(corrected);
            var assigned = new HashSet<Transform>();
            var adjusted = 0;

            for (var index = 0; index < ExpectedWaypoints; index++)
            {
                var previous = Flat(waypoints[(index - 1 + ExpectedWaypoints) % ExpectedWaypoints].position);
                var p = Flat(waypoints[index].position);
                var next = Flat(waypoints[(index + 1) % ExpectedWaypoints].position);
                var after = Flat(waypoints[(index + 2) % ExpectedWaypoints].position);

                var incoming = SafeDirection(p - previous);
                var direction = SafeDirection(next - p);
                var outgoing = SafeDirection(after - next);
                if (incoming == Vector3.zero || direction == Vector3.zero || outgoing == Vector3.zero)
                    return 0;

                var midpoint = (p + next) * .5f;
                var right = RightOf(direction);

                for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    var side = sideIndex == 0 ? 1f : -1f;
                    var idealCenter = midpoint + right * (RailLateralOffset * side);
                    var rail = FindBestRail(available, assigned, idealCenter, direction);
                    if (rail == null) return 0;

                    ResolveSignedSpan(previous, p, next, after, side, out var start, out var end);
                    var span = end - start;
                    var length = span.magnitude;
                    if (length < 1f) return 0;

                    var currentY = rail.position.y;
                    var center = (start + end) * .5f;
                    var rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, -90f, 0f);
                    rail.SetPositionAndRotation(new Vector3(center.x, currentY, center.z), rotation);

                    var scale = rail.localScale;
                    scale.x = Mathf.Max(.5f, length * .5f / AuthoredHalfLengthMeters);
                    rail.localScale = scale;

                    assigned.Add(rail);
                    adjusted++;
                }
            }

            return adjusted;
        }

        private static Transform FindBestRail(
            IReadOnlyList<Transform> candidates,
            HashSet<Transform> assigned,
            Vector3 idealCenter,
            Vector3 segmentDirection)
        {
            Transform best = null;
            var bestScore = float.PositiveInfinity;

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate == null || assigned.Contains(candidate)) continue;

                var candidateDirection = Flat(candidate.right).normalized;
                var alignment = Mathf.Abs(Vector3.Dot(candidateDirection, segmentDirection));
                if (alignment < MinimumDirectionAlignment) continue;

                var delta = Flat(candidate.position) - idealCenter;
                var score = delta.sqrMagnitude + (1f - alignment) * 100f;
                if (score >= bestScore) continue;

                bestScore = score;
                best = candidate;
            }

            return best;
        }

        private static void ResolveSignedSpan(
            Vector3 previous,
            Vector3 p,
            Vector3 next,
            Vector3 after,
            float side,
            out Vector3 start,
            out Vector3 end)
        {
            var incoming = SafeDirection(p - previous);
            var direction = SafeDirection(next - p);
            var outgoing = SafeDirection(after - next);

            var currentRight = RightOf(direction);
            var previousRight = RightOf(incoming);
            var outgoingRight = RightOf(outgoing);
            var offset = RailLateralOffset * side;

            var startBase = p + currentRight * offset;
            var previousBase = p + previousRight * offset;
            start = IntersectOffsetLines(startBase, direction, previousBase, incoming, startBase);

            var endBase = next + currentRight * offset;
            var outgoingBase = next + outgoingRight * offset;
            end = IntersectOffsetLines(endBase, direction, outgoingBase, outgoing, endBase);
        }

        private static Vector3 IntersectOffsetLines(
            Vector3 currentBase,
            Vector3 currentDirection,
            Vector3 adjacentBase,
            Vector3 adjacentDirection,
            Vector3 fallback)
        {
            var denominator = Cross2(currentDirection, adjacentDirection);
            if (Mathf.Abs(denominator) < .0001f) return fallback;

            var delta = adjacentBase - currentBase;
            var alongCurrent = Cross2(delta, adjacentDirection) / denominator;
            alongCurrent = Mathf.Clamp(alongCurrent, -MaxJoinShiftMeters, MaxJoinShiftMeters);
            return currentBase + currentDirection * alongCurrent;
        }

        private static float Cross2(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;

        private static Vector3 RightOf(Vector3 direction)
        {
            var right = Vector3.Cross(Vector3.up, direction);
            return right.sqrMagnitude < .0001f ? Vector3.zero : right.normalized;
        }

        private static Vector3 SafeDirection(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude < .0001f ? Vector3.zero : value.normalized;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static bool IsRaceRail(Transform candidate)
        {
            if (candidate == null || candidate.name != BarrierName) return false;
            if (candidate.parent == null || candidate.parent.name != TrackRootName) return false;
            return Mathf.Abs(candidate.localScale.x) > MinimumRailScaleX;
        }
    }
}
