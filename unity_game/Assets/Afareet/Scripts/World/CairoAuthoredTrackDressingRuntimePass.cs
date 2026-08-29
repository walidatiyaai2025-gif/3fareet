using System;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Replaces the visible UART-007 primitive track dressing after CairoTrackBuilder has
    /// created the stable physics/layout objects. Existing colliders are preserved while
    /// player-side primitive renderers are never accepted as production presentation.
    /// </summary>
    public sealed class CairoAuthoredTrackDressingRuntimePass : MonoBehaviour
    {
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO AUTHORED TRACK DRESSING RUNTIME PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoAuthoredTrackDressingRuntimePass>();
        }

        private void Update()
        {
            if (complete) return;

            var trackRoot = GameObject.Find("CAIRO NIGHT RUN // 3FAREET");
            var w0 = FindWaypoint(0);
            var w18 = FindWaypoint(18);
            var w36 = FindWaypoint(36);
            var w54 = FindWaypoint(54);
            if (trackRoot == null || w0 == null || w18 == null || w36 == null || w54 == null) return;

            var dark = RuntimeMaterials.Lit(new Color(.025f, .018f, .04f), .14f, .42f);
            var desert = RuntimeMaterials.Lit(new Color(.12f, .065f, .055f), .06f, .30f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .88f, 3.6f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .16f, .86f, 4f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .5f, .05f), .22f, .88f, 3f);
            var magenta = RuntimeMaterials.Lit(new Color(1f, .06f, .48f), .18f, .86f, 3.5f);

            var groundOk = CairoAuthoredTrackDressing.TryCreateGround(trackRoot.transform, desert, purple);
            ResolveLegacyRenderer("Desert Ground", groundOk, "GROUND");

            var finishOk = CairoAuthoredTrackDressing.TryCreateFinishGate(trackRoot.transform, w0, cyan, purple, gold);
            ResolveLegacyRenderer("Finish Left", finishOk, "FINISH_GATE");
            ResolveLegacyRenderer("Finish Right", finishOk, "FINISH_GATE");
            ResolveLegacyRenderer("Finish Beam", finishOk, "FINISH_GATE");
            ResolveLegacyRenderer("Finish Spirit Blade", finishOk, "FINISH_GATE");
            ResolveLegacyRenderer("Finish Gold Blade", finishOk, "FINISH_GATE");

            var allRunesOk = true;
            for (var i = 0; i < 72; i += 6)
            {
                var current = FindWaypoint(i);
                var next = FindWaypoint((i + 1) % 72);
                if (current == null || next == null)
                {
                    allRunesOk = false;
                    continue;
                }

                var position = (current.position + next.position) * .5f + Vector3.up * .22f;
                var primary = i % 12 == 0 ? gold : purple;
                if (!CairoAuthoredTrackDressing.TryCreateRoadRune(trackRoot.transform, position, current.rotation, primary, gold))
                    allRunesOk = false;
            }
            ResolveLegacyPrefix("Asphalt Spirit Rune", allRunesOk, "ROAD_RUNE");

            var beaconsOk = true;
            beaconsOk &= CairoAuthoredTrackDressing.TryCreateSectorBeacon(trackRoot.transform, w0, gold, purple, dark, gold);
            beaconsOk &= CairoAuthoredTrackDressing.TryCreateSectorBeacon(trackRoot.transform, w18, cyan, purple, dark, gold);
            beaconsOk &= CairoAuthoredTrackDressing.TryCreateSectorBeacon(trackRoot.transform, w36, gold, cyan, dark, gold);
            beaconsOk &= CairoAuthoredTrackDressing.TryCreateSectorBeacon(trackRoot.transform, w54, purple, cyan, dark, magenta);
            ResolveLegacyPrefix("Sector Beacon //", beaconsOk, "SECTOR_BEACON");

            if (groundOk && finishOk && allRunesOk && beaconsOk)
                Debug.Log("AFAREET_UART007_AUTHORED_TRACK_DRESSING_RUNTIME_OK ground=true finish=true runes=true beacons=true");
            else if (!Application.isEditor)
                Debug.LogError($"AFAREET_UART007_AUTHORED_TRACK_DRESSING_INCOMPLETE ground={groundOk} finish={finishOk} runes={allRunesOk} beacons={beaconsOk}");

            complete = true;
        }

        private static Transform FindWaypoint(int index)
        {
            var waypoint = GameObject.Find($"Waypoint {index:00}");
            return waypoint == null ? null : waypoint.transform;
        }

        private static void ResolveLegacyRenderer(string exactName, bool authoredActive, string marker)
        {
            var legacy = GameObject.Find(exactName);
            if (legacy == null) return;
            var shouldHide = authoredActive || !Application.isEditor;
            if (!shouldHide) return;
            SetRenderersEnabled(legacy, false);
            if (!authoredActive)
                Debug.LogError($"AFAREET_UART007_PLAYER_PRIMITIVE_{marker}_FALLBACK_DISABLED name={exactName}");
        }

        private static void ResolveLegacyPrefix(string prefix, bool authoredActive, string marker)
        {
            var shouldHide = authoredActive || !Application.isEditor;
            if (!shouldHide) return;

            var matched = 0;
            foreach (var candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                SetRenderersEnabled(candidate, false);
                matched++;
            }

            if (!authoredActive)
                Debug.LogError($"AFAREET_UART007_PLAYER_PRIMITIVE_{marker}_FALLBACK_DISABLED matches={matched}");
        }

        private static void SetRenderersEnabled(GameObject root, bool enabled)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer != null) renderer.enabled = enabled;
        }
    }
}
