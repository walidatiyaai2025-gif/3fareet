using System;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Replaces the two legacy procedural CairoTrackBuilder pyramids with the tracked
    /// UART-006 authored pyramid Resource. In a Player build, primitive pyramid renderers
    /// are never left visible when the authored source is unavailable.
    /// </summary>
    public sealed class CairoTrackPyramidAuthoredReplacementPass : MonoBehaviour
    {
        private const string PyramidPath = "Art/Architecture/CairoLandmarks/Generated/SM_Landmark_GizaPyramid_A";
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO TRACK PYRAMID AUTHORED REPLACEMENT PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoTrackPyramidAuthoredReplacementPass>();
        }

        private void Update()
        {
            if (complete) return;
            var trackRoot = GameObject.Find("CAIRO NIGHT RUN // 3FAREET");
            if (trackRoot == null) return;

            var source = Resources.Load<GameObject>(PyramidPath);
            if (source == null)
            {
                if (!Application.isEditor)
                {
                    HideLegacyPyramidRenderers();
                    Debug.LogError($"AFAREET_UART006_PLAYER_TRACK_PYRAMID_RESOURCE_MISSING path={PyramidPath}");
                    Debug.LogError("AFAREET_UART006_PLAYER_PRIMITIVE_TRACK_PYRAMID_FALLBACK_DISABLED");
                    complete = true;
                }
                return;
            }

            var dark = RuntimeMaterials.Lit(new Color(.20f, .105f, .055f), .15f, .40f);
            var spirit = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .16f, .86f, 4f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .06f), .22f, .88f, 3f);

            Create(source, trackRoot.transform, new Vector3(-34f, 0f, -16f), 1.72f, dark, spirit, gold);
            Create(source, trackRoot.transform, new Vector3(-8f, 0f, -22f), 1.20f, dark, spirit, gold);
            DestroyLegacyPyramids();
            Debug.Log("AFAREET_UART006_TRACK_PYRAMIDS_REPLACED source=tracked-obj legacy-procedural-destroyed");
            complete = true;
        }

        private static void Create(GameObject source, Transform parent, Vector3 worldPosition, float scale, Material dark, Material spirit, Material gold)
        {
            var instance = Instantiate(source, parent, false);
            instance.name = "AUTHORED GIZA TRACK PYRAMID";
            instance.transform.position = worldPosition;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;

            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                var n = renderer.gameObject.name ?? string.Empty;
                var selected = dark;
                if (Contains(n, "Apex") || Contains(n, "Gold") || Contains(n, "Crown")) selected = gold;
                else if (Contains(n, "Spirit") || Contains(n, "Portal") || Contains(n, "Channel")) selected = spirit;
                var count = Mathf.Max(1, renderer.sharedMaterials == null ? 0 : renderer.sharedMaterials.Length);
                var bindings = new Material[count];
                for (var i = 0; i < bindings.Length; i++) bindings[i] = selected;
                renderer.sharedMaterials = bindings;
            }
        }

        private static bool Contains(string value, string token) =>
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void DestroyLegacyPyramids()
        {
            foreach (var candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (candidate == null) continue;
                if (candidate.name == "Giza Spirit Pyramid" || candidate.name == "Pyramid Spirit Crown")
                    Destroy(candidate);
            }
        }

        private static void HideLegacyPyramidRenderers()
        {
            foreach (var candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (candidate == null) continue;
                if (candidate.name != "Giza Spirit Pyramid" && candidate.name != "Pyramid Spirit Crown") continue;
                foreach (var renderer in candidate.GetComponentsInChildren<Renderer>(true))
                    if (renderer != null) renderer.enabled = false;
            }
        }
    }
}
