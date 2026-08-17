using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Editor-only visual diagnostic for the local P1 art review loop. It reports the
    /// largest visible renderers and separately ranks tall renderers whose projected bounds
    /// overlap the center driving sightline. No production/runtime gate is changed.
    /// </summary>
    public sealed class EditorCameraVisibilityDiagnostic : MonoBehaviour
    {
#if UNITY_EDITOR
        private const int MaxReported = 8;
        private const int MaxCenterReported = 6;
        private const float MinimumWarmupSeconds = 0.75f;
        private static readonly Rect CenterSightline = new(.32f, .34f, .36f, .46f);
        private bool reported;
        private float readyAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<EditorCameraVisibilityDiagnostic>() != null) return;
            var host = new GameObject("AFAREET EDITOR CAMERA VISIBILITY DIAGNOSTIC");
            DontDestroyOnLoad(host);
            host.AddComponent<EditorCameraVisibilityDiagnostic>();
        }

        private void Awake()
        {
            readyAt = Time.realtimeSinceStartup + MinimumWarmupSeconds;
        }

        private void Update()
        {
            if (reported || Time.realtimeSinceStartup < readyAt) return;

            var cameraObject = GameObject.Find("Racing Camera");
            var hero = GameObject.Find("PLAYER HERO — AFAREET");
            if (cameraObject == null || hero == null) return;

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null || !camera.enabled) return;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var cameraPosition = camera.transform.position;
            var candidates = new List<VisibleCandidate>();
            var centerCandidates = new List<CenterCandidate>();
            var heroHeight = hero.transform.position.y;

            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;

                var bounds = renderer.bounds;
                var distance = Mathf.Max(.05f, Vector3.Distance(cameraPosition, bounds.center));
                var maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                var apparentScore = maxDimension / distance;
                if (apparentScore >= .03f)
                    candidates.Add(new VisibleCandidate(renderer, distance, maxDimension, apparentScore));

                // Ground/road surfaces can dominate the generic apparent-size ranking while not
                // being the actual skyline blocker. Center-sightline ranking therefore requires
                // meaningful vertical extent above the Hero and projected overlap with the center.
                if (bounds.max.y <= heroHeight + 2.25f || bounds.size.y < 1.5f) continue;
                if (!TryProjectedViewportRect(camera, bounds, out var viewportRect)) continue;
                var overlap = IntersectionArea(viewportRect, CenterSightline);
                if (overlap <= .001f) continue;

                var coverage = overlap / Mathf.Max(.001f, CenterSightline.width * CenterSightline.height);
                var centerScore = coverage * (1f + 1f / Mathf.Max(1f, distance));
                centerCandidates.Add(new CenterCandidate(renderer, distance, viewportRect, coverage, centerScore));
            }

            candidates.Sort((a, b) => b.ApparentScore.CompareTo(a.ApparentScore));
            var count = Mathf.Min(MaxReported, candidates.Count);
            Debug.Log(
                $"AFAREET_EDITOR_CAMERA_VISIBILITY_SCAN camera=Racing Camera visibleCandidates={candidates.Count} " +
                $"reported={count} production=false");

            for (var i = count - 1; i >= 0; i--)
            {
                var candidate = candidates[i];
                var renderer = candidate.Renderer;
                var owner = TopLevelRuntimeOwner(renderer.transform);
                var parent = renderer.transform.parent == null ? "<root>" : renderer.transform.parent.name;
                var size = renderer.bounds.size;
                Debug.Log(
                    $"AFAREET_EDITOR_CAMERA_VISIBLE_CULPRIT rank={i + 1} owner={owner} parent={parent} renderer={renderer.gameObject.name} " +
                    $"size=({size.x:F2},{size.y:F2},{size.z:F2})m maxDimension={candidate.MaxDimension:F2}m " +
                    $"distance={candidate.Distance:F2}m apparent={candidate.ApparentScore:F3} production=false");
            }

            centerCandidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            var centerCount = Mathf.Min(MaxCenterReported, centerCandidates.Count);
            Debug.Log(
                $"AFAREET_EDITOR_CAMERA_CENTER_SCAN candidates={centerCandidates.Count} reported={centerCount} " +
                $"sightline=({CenterSightline.x:F2},{CenterSightline.y:F2},{CenterSightline.width:F2},{CenterSightline.height:F2}) production=false");

            // As with the generic scan, emit rank=1 last so it is immediately visible in Console.
            for (var i = centerCount - 1; i >= 0; i--)
            {
                var candidate = centerCandidates[i];
                var renderer = candidate.Renderer;
                var owner = TopLevelRuntimeOwner(renderer.transform);
                var parent = renderer.transform.parent == null ? "<root>" : renderer.transform.parent.name;
                var rect = candidate.ViewportRect;
                Debug.Log(
                    $"AFAREET_EDITOR_CAMERA_CENTER_OCCLUDER rank={i + 1} owner={owner} parent={parent} renderer={renderer.gameObject.name} " +
                    $"distance={candidate.Distance:F2}m coverage={candidate.Coverage:F3} " +
                    $"viewport=({rect.xMin:F2},{rect.yMin:F2})-({rect.xMax:F2},{rect.yMax:F2}) score={candidate.Score:F3} production=false");
            }

            reported = true;
        }

        private static bool TryProjectedViewportRect(Camera camera, Bounds bounds, out Rect rect)
        {
            var c = bounds.center;
            var e = bounds.extents;
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            var frontPoints = 0;

            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var world = c + Vector3.Scale(e, new Vector3(x, y, z));
                var viewport = camera.WorldToViewportPoint(world);
                if (viewport.z <= 0f) continue;
                frontPoints++;
                minX = Mathf.Min(minX, viewport.x);
                minY = Mathf.Min(minY, viewport.y);
                maxX = Mathf.Max(maxX, viewport.x);
                maxY = Mathf.Max(maxY, viewport.y);
            }

            if (frontPoints == 0 || minX > maxX || minY > maxY)
            {
                rect = default;
                return false;
            }

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static float IntersectionArea(Rect a, Rect b)
        {
            var minX = Mathf.Max(a.xMin, b.xMin);
            var minY = Mathf.Max(a.yMin, b.yMin);
            var maxX = Mathf.Min(a.xMax, b.xMax);
            var maxY = Mathf.Min(a.yMax, b.yMax);
            return Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxY - minY);
        }

        private static string TopLevelRuntimeOwner(Transform transform)
        {
            if (transform == null) return "<null>";
            var cursor = transform;
            while (cursor.parent != null && cursor.parent.name != "AFAREET_RUNTIME")
                cursor = cursor.parent;
            return cursor.name;
        }

        private readonly struct VisibleCandidate
        {
            public readonly Renderer Renderer;
            public readonly float Distance;
            public readonly float MaxDimension;
            public readonly float ApparentScore;

            public VisibleCandidate(Renderer renderer, float distance, float maxDimension, float apparentScore)
            {
                Renderer = renderer;
                Distance = distance;
                MaxDimension = maxDimension;
                ApparentScore = apparentScore;
            }
        }

        private readonly struct CenterCandidate
        {
            public readonly Renderer Renderer;
            public readonly float Distance;
            public readonly Rect ViewportRect;
            public readonly float Coverage;
            public readonly float Score;

            public CenterCandidate(Renderer renderer, float distance, Rect viewportRect, float coverage, float score)
            {
                Renderer = renderer;
                Distance = distance;
                ViewportRect = viewportRect;
                Coverage = coverage;
                Score = score;
            }
        }
#endif
    }
}
