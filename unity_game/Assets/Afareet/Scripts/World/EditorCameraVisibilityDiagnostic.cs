using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Editor-only visual diagnostic for the local P1 art review loop. It reports the
    /// largest renderers currently visible to the authoritative Racing Camera so an
    /// oversized/misplaced authored preview can be identified without weakening any
    /// production, device or owner-acceptance gate.
    /// </summary>
    public sealed class EditorCameraVisibilityDiagnostic : MonoBehaviour
    {
#if UNITY_EDITOR
        private const int MaxReported = 8;
        private const float MinimumWarmupSeconds = 0.75f;
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

            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;

                var bounds = renderer.bounds;
                var distance = Mathf.Max(.05f, Vector3.Distance(cameraPosition, bounds.center));
                var maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                var apparentScore = maxDimension / distance;
                if (apparentScore < .03f) continue;

                candidates.Add(new VisibleCandidate(renderer, distance, maxDimension, apparentScore));
            }

            candidates.Sort((a, b) => b.ApparentScore.CompareTo(a.ApparentScore));
            var count = Mathf.Min(MaxReported, candidates.Count);
            Debug.Log(
                $"AFAREET_EDITOR_CAMERA_VISIBILITY_SCAN camera=Racing Camera visibleCandidates={candidates.Count} " +
                $"reported={count} production=false");

            for (var i = 0; i < count; i++)
            {
                var candidate = candidates[i];
                var renderer = candidate.Renderer;
                var owner = TopLevelRuntimeOwner(renderer.transform);
                var size = renderer.bounds.size;
                Debug.Log(
                    $"AFAREET_EDITOR_CAMERA_VISIBLE_CULPRIT rank={i + 1} owner={owner} renderer={renderer.gameObject.name} " +
                    $"size=({size.x:F2},{size.y:F2},{size.z:F2})m maxDimension={candidate.MaxDimension:F2}m " +
                    $"distance={candidate.Distance:F2}m apparent={candidate.ApparentScore:F3} production=false");
            }

            reported = true;
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
#endif
    }
}
