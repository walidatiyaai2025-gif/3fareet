using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Corrects the authored UART-005 rail placement contract at runtime.
    /// SM_Prop_CairoBarrier_A is authored 2m long on local X (-1..+1). TrackBuilder
    /// supplies a segment-start position and scales local X to half the desired segment
    /// length, so long race rails must be shifted by their scaled authored half-length
    /// along local +X after the -90 degree alignment rotation. Small decorative barriers
    /// (for example lamp/totem accents) are intentionally excluded.
    /// </summary>
    public sealed class CairoAuthoredRailCenteringPass : MonoBehaviour
    {
        private const string TrackRootName = "CAIRO NIGHT RUN // 3FAREET";
        private const string BarrierName = "AUTHORED CAIRO BARRIER";
        private const float AuthoredHalfLengthMeters = 1f;
        private const float MinimumRailScaleX = 1.25f;
        private const int ExpectedRaceRails = 72 * 2;

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

            var changedThisScan = 0;
            foreach (var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!IsRaceRail(candidate) || corrected.Contains(candidate)) continue;

                var halfLength = Mathf.Abs(candidate.localScale.x) * AuthoredHalfLengthMeters;
                candidate.position += candidate.right * halfLength;
                corrected.Add(candidate);
                changedThisScan++;
            }

            if (corrected.Count < ExpectedRaceRails) return;

            completionLogged = true;
            Debug.Log(
                $"AFAREET_UART005_AUTHORED_RAIL_CENTERING_ACTIVE corrected={corrected.Count} " +
                $"expected={ExpectedRaceRails} sourceHalfLength={AuthoredHalfLengthMeters:F1}m " +
                "placement=centered-on-segment primitiveGeometry=false");
        }

        private static bool IsRaceRail(Transform candidate)
        {
            if (candidate == null || candidate.name != BarrierName) return false;
            if (candidate.parent == null || candidate.parent.name != TrackRootName) return false;
            return Mathf.Abs(candidate.localScale.x) > MinimumRailScaleX;
        }
    }
}
