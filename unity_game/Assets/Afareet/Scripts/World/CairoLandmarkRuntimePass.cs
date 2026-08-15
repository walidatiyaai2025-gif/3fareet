using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoLandmarkRuntimePass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO LANDMARK RUNTIME PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoLandmarkRuntimePass>();
        }

        private void Update()
        {
            if (built) return;
            var w0 = FindWaypoint(0);
            var w18 = FindWaypoint(18);
            var w36 = FindWaypoint(36);
            var w54 = FindWaypoint(54);
            var w66 = FindWaypoint(66);
            if (w0 == null || w18 == null || w36 == null || w54 == null || w66 == null) return;

            var dark = RuntimeMaterials.Lit(new Color(.016f, .013f, .03f), .12f, .4f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .16f, .86f, 4f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .88f, 3.6f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .5f, .05f), .22f, .88f, 3f);

            var minarets = CairoAuthoredLandmarkKit.TryBuildMinarets(w18, dark, purple, cyan, gold);
            var dome = CairoAuthoredLandmarkKit.TryBuildDomeGate(w36, dark, purple, cyan, gold);
            var pyramids = CairoAuthoredLandmarkKit.TryBuildPyramidPair(w54, dark, purple, gold);
            var bridge = CairoAuthoredLandmarkKit.TryBuildBridgeGantry(w66, dark, purple, cyan, gold);

            if (Application.isEditor)
            {
                if (!minarets) BuildMinarets(w18, dark, purple, cyan, gold);
                if (!dome) BuildDomeGate(w36, dark, purple, cyan, gold);
                if (!pyramids) BuildPyramidPair(w54, dark, purple, gold);
                if (!bridge) BuildBridgeGantry(w66, dark, purple, cyan, gold);

                // Sector beacons are retained only as explicit Editor dressing placeholders.
                BuildSectorBeacon(w0, "ROYAL START", gold, purple, dark);
                BuildSectorBeacon(w18, "NEON CITY", cyan, purple, dark);
                BuildSectorBeacon(w36, "GIZA GOLD", gold, cyan, dark);
                BuildSectorBeacon(w54, "SPIRIT RETURN", purple, cyan, dark);
            }
            else
            {
                if (!minarets) PlayerAuthoredMissing("minaret-cluster");
                if (!dome) PlayerAuthoredMissing("dome-gate");
                if (!pyramids) PlayerAuthoredMissing("pyramid-pair");
                if (!bridge) PlayerAuthoredMissing("bridge-gantry");
                Debug.LogWarning("AFAREET_UART007_SECTOR_BEACONS_PENDING_AUTHORED_ART primitive-player-path-disabled");
            }

            if (minarets && dome && pyramids && bridge)
                Debug.Log("AFAREET_UART006_PLAYER_AUTHORED_LANDMARK_PASS_ACTIVE sources=4 primitive-landmark-fallback=false");

            built = true;
        }

        private static void PlayerAuthoredMissing(string landmark)
        {
            Debug.LogError($"AFAREET_UART006_PLAYER_PRIMITIVE_LANDMARK_FALLBACK_DISABLED landmark={landmark}");
        }

        private static Transform FindWaypoint(int index)
        {
            var waypoint = GameObject.Find($"Waypoint {index:00}");
            return waypoint == null ? null : waypoint.transform;
        }

        // Everything below this line is Editor-only fallback geometry for development.
        // Player builds never call these helpers.
        private static void BuildMinarets(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var root = Root("DEV Spirit Minaret Cluster", anchor, -anchor.right * 26f + Vector3.up * .2f);
            Tower(root, new Vector3(-6f, 0f, 0f), 11f, dark, purple, gold);
            Tower(root, Vector3.zero, 15f, dark, cyan, gold);
            Tower(root, new Vector3(6f, 0f, 0f), 9f, dark, purple, gold);
        }

        private static void Tower(Transform root, Vector3 p, float height, Material body, Material glow, Material gold)
        {
            Part(root, "Minaret Body", PrimitiveType.Cylinder, p + Vector3.up * height * .5f, new Vector3(1.8f, height * .5f, 1.8f), body);
            Part(root, "Minaret Balcony", PrimitiveType.Cylinder, p + Vector3.up * (height * .72f), new Vector3(2.6f, .25f, 2.6f), glow);
            Part(root, "Minaret Crown", PrimitiveType.Cylinder, p + Vector3.up * (height + .7f), new Vector3(1.3f, 1.4f, 1.3f), glow);
            Part(root, "Minaret Gold Tip", PrimitiveType.Cylinder, p + Vector3.up * (height + 2.1f), new Vector3(.28f, 1.4f, .28f), gold);
        }

        private static void BuildDomeGate(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var root = Root("DEV Neon Dome Gate", anchor, anchor.right * 24f);
            Part(root, "Gate Left", PrimitiveType.Cube, new Vector3(-5.5f, 3.6f, 0f), new Vector3(1.2f, 7.2f, 1.2f), dark);
            Part(root, "Gate Right", PrimitiveType.Cube, new Vector3(5.5f, 3.6f, 0f), new Vector3(1.2f, 7.2f, 1.2f), dark);
            Part(root, "Gate Beam", PrimitiveType.Cube, new Vector3(0f, 7f, 0f), new Vector3(12f, .7f, 1.2f), gold);
            Part(root, "Dome Base", PrimitiveType.Cylinder, new Vector3(0f, 8.2f, 0f), new Vector3(4.2f, .7f, 4.2f), dark);
            Part(root, "Dome Purple", PrimitiveType.Sphere, new Vector3(0f, 10.1f, 0f), new Vector3(6.8f, 3.7f, 6.8f), purple);
            Part(root, "Dome Cyan Ring", PrimitiveType.Cylinder, new Vector3(0f, 9.2f, 0f), new Vector3(5.1f, .18f, 5.1f), cyan);
            Part(root, "Dome Gold Spire", PrimitiveType.Cylinder, new Vector3(0f, 12.7f, 0f), new Vector3(.3f, 2.2f, .3f), gold);
        }

        private static void BuildPyramidPair(Transform anchor, Material dark, Material purple, Material gold)
        {
            var root = Root("DEV Pyramid Horizon Pair", anchor, -anchor.right * 40f);
            Pyramid(root, new Vector3(-9f, 0f, 1.5f), 12f, dark, purple, gold);
            Pyramid(root, new Vector3(8f, 0f, -2f), 8.5f, dark, purple, gold);
        }

        private static void Pyramid(Transform root, Vector3 p, float width, Material dark, Material glow, Material gold)
        {
            const int layers = 5;
            for (var layer = 0; layer < layers; layer++)
            {
                var t = layer / (float)layers;
                var size = width * (1f - t * .78f);
                var height = width * .11f;
                var material = layer == layers - 1 ? glow : dark;
                Part(root, $"Pyramid Layer {layer + 1}", PrimitiveType.Cube, p + Vector3.up * (height * .5f + layer * height), new Vector3(size, height, size), material);
            }
            Part(root, "Pyramid Gold Apex", PrimitiveType.Cube, p + Vector3.up * (width * .66f), new Vector3(width * .12f, width * .16f, width * .12f), gold);
        }

        private static void BuildBridgeGantry(Transform anchor, Material dark, Material purple, Material cyan, Material gold)
        {
            var root = Root("DEV Cairo Bridge Gantry", anchor, Vector3.zero);
            Part(root, "Left Tower", PrimitiveType.Cube, new Vector3(-7f, 3.8f, 0f), new Vector3(1.1f, 7.6f, 1.1f), dark);
            Part(root, "Right Tower", PrimitiveType.Cube, new Vector3(7f, 3.8f, 0f), new Vector3(1.1f, 7.6f, 1.1f), dark);
            Part(root, "Bridge Beam", PrimitiveType.Cube, new Vector3(0f, 7f, 0f), new Vector3(15f, .8f, 1.1f), dark);
            Part(root, "Purple Rail", PrimitiveType.Cube, new Vector3(0f, 7.6f, -.18f), new Vector3(13.2f, .18f, .22f), purple);
            Part(root, "Cyan Rail", PrimitiveType.Cube, new Vector3(0f, 6.45f, -.18f), new Vector3(13.2f, .18f, .22f), cyan);
            Part(root, "Gold Crest", PrimitiveType.Cube, new Vector3(0f, 8.35f, 0f), new Vector3(4f, .25f, .25f), gold);
        }

        private static void BuildSectorBeacon(Transform anchor, string label, Material primary, Material secondary, Material dark)
        {
            var root = Root($"DEV Sector Beacon // {label}", anchor, anchor.right * 18f);
            Part(root, "Sector Mast", PrimitiveType.Cube, new Vector3(0f, 2.4f, 0f), new Vector3(.32f, 4.8f, .32f), dark);
            Part(root, "Primary Blade", PrimitiveType.Cube, new Vector3(-.55f, 4f, 0f), new Vector3(.16f, 2.4f, .42f), primary, Quaternion.Euler(0f, 0f, -11f));
            Part(root, "Secondary Blade", PrimitiveType.Cube, new Vector3(.55f, 4f, 0f), new Vector3(.16f, 2.4f, .42f), secondary, Quaternion.Euler(0f, 0f, 11f));
            Part(root, "Sector Crown", PrimitiveType.Cube, new Vector3(0f, 5.35f, 0f), new Vector3(2.2f, .14f, .34f), primary);
        }

        private static Transform Root(string name, Transform anchor, Vector3 offset)
        {
            var root = new GameObject(name).transform;
            root.position = anchor.position + offset;
            root.rotation = anchor.rotation;
            return root;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material, Quaternion? localRotation = null)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation ?? Quaternion.identity;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
        }
    }
}
