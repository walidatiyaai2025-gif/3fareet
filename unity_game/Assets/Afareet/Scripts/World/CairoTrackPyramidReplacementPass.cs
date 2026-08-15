using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Removes the legacy procedural pyramids created by CairoTrackBuilder once the
    /// tracked UART-006 pyramid resource is available, then installs authored sources.
    /// This keeps the physical track build stable while removing primitive presentation.
    /// </summary>
    public sealed class CairoTrackPyramidReplacementPass : MonoBehaviour
    {
        private const string PyramidResourcePath = "Art/TracksEnvironments/CairoLandmarks/Generated/SM_Landmark_GizaSpiritPyramid_A";
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO TRACK PYRAMID REPLACEMENT PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoTrackPyramidReplacementPass>();
        }

        private void Update()
        {
            if (complete) return;
            var trackRoot = GameObject.Find("CAIRO NIGHT RUN // 3FAREET");
            if (trackRoot == null) return;

            var source = Resources.Load<GameObject>(PyramidResourcePath);
            if (source == null)
            {
                if (!Application.isEditor)
                {
                    Debug.LogError($"AFAREET_UART006_PLAYER_TRACK_PYRAMID_RESOURCE_MISSING path={PyramidResourcePath}");
                    DisableLegacyPyramidRenderers();
                    complete = true;
                }
                return;
            }

            var dark = RuntimeMaterials.Lit(new Color(.20f, .105f, .055f), .15f, .40f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .16f, .86f, 4f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .06f), .22f, .88f, 3f);

            var first = CairoAuthoredLandmarks.TryCreateTrackPyramid(trackRoot.transform, new Vector3(-34f, 0f, -16f), 1.72f, dark, purple, gold);
            var second = CairoAuthoredLandmarks.TryCreateTrackPyramid(trackRoot.transform, new Vector3(-8f, 0f, -22f), 1.20f, dark, purple, gold);
            if (!first || !second)
            {
                if (!Application.isEditor)
                    DisableLegacyPyramidRenderers();
                return;
            }

            DestroyLegacyPyramids();
            Debug.Log("AFAREET_UART006_TRACK_PYRAMIDS_REPLACED source=tracked-obj legacy-procedural-destroyed");
            complete = true;
        }

        private static void DestroyLegacyPyramids()
        {
            foreach (var candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (candidate == null) continue;
                if (candidate.name == "Giza Spirit Pyramid" || candidate.name == "Pyramid Spirit Crown")
                    Object.Destroy(candidate);
            }
        }

        private static void DisableLegacyPyramidRenderers()
        {
            foreach (var candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (candidate == null) continue;
                if (candidate.name != "Giza Spirit Pyramid" && candidate.name != "Pyramid Spirit Crown") continue;
                foreach (var renderer in candidate.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }
            Debug.LogError("AFAREET_UART006_PLAYER_PRIMITIVE_TRACK_PYRAMID_FALLBACK_DISABLED");
        }
    }
}
