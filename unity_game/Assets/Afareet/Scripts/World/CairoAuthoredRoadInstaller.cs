using System;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Replaces the visible procedural road cubes with the tracked UART-005 authored
    /// road/curb OBJ while preserving the existing invisible collision/layout scaffold.
    /// Player builds never restore the rejected primitive road renderer.
    /// </summary>
    public sealed class CairoAuthoredRoadInstaller : MonoBehaviour
    {
        private const string TrackRootName = "CAIRO NIGHT RUN // 3FAREET";
        private const string RoadPrefix = "Road ";
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("Cairo Authored Road Installer");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoAuthoredRoadInstaller>();
        }

        private void Update()
        {
            if (complete) return;

            var trackRootObject = GameObject.Find(TrackRootName);
            if (trackRootObject == null) return;

            var asphalt = RuntimeMaterials.Lit(new Color(.022f, .028f, .052f), .18f, .58f);
            var curb = RuntimeMaterials.Lit(new Color(.18f, .13f, .22f), .12f, .38f);
            var edge = RuntimeMaterials.Lit(new Color(.08f, .72f, 1f), .25f, .86f, 3.2f);

            var replaced = 0;
            var failed = 0;
            var roads = trackRootObject.GetComponentsInChildren<Transform>(true);
            foreach (var road in roads)
            {
                if (road == null || !road.name.StartsWith(RoadPrefix, StringComparison.Ordinal)) continue;

                var primitiveRenderer = road.GetComponent<MeshRenderer>();
                if (primitiveRenderer == null) continue;

                var length = Mathf.Abs(road.localScale.z);
                var position = road.position;
                var rotation = road.rotation;
                primitiveRenderer.enabled = false;

                if (CairoAuthoredStreetKit.TryCreateRoadSegment(
                        trackRootObject.transform,
                        position,
                        rotation,
                        length,
                        asphalt,
                        curb,
                        edge))
                {
                    replaced++;
                    continue;
                }

                failed++;
                if (Application.isEditor)
                {
                    primitiveRenderer.enabled = true;
                    Debug.LogWarning($"AFAREET_UART005_EDITOR_ROAD_BLOCKOUT_FALLBACK_ACTIVE road={road.name}");
                }
                else
                {
                    Debug.LogError($"AFAREET_UART005_PLAYER_PRIMITIVE_ROAD_FALLBACK_DISABLED road={road.name}");
                }
            }

            if (replaced == 0 && failed == 0) return;

            complete = true;
            if (failed > 0)
            {
                Debug.LogError($"AFAREET_UART005_AUTHORED_ROAD_INCOMPLETE replaced={replaced} failed={failed}");
                return;
            }

            Debug.Log($"AFAREET_UART005_AUTHORED_ROAD_ACTIVE replaced={replaced} source=tracked-obj primitive-renderers=disabled");
        }
    }
}
