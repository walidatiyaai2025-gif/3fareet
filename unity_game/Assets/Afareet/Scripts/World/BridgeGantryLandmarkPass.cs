using UnityEngine;

namespace Afareet.World
{
    public sealed class BridgeGantryLandmarkPass : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("CAIRO BRIDGE GANTRY PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<BridgeGantryLandmarkPass>();
        }

        private bool built;

        private void Update()
        {
            if (built) return;
            var track = FindFirstObjectByType<TrackRuntime>();
            if (track == null || track.Waypoints == null || track.Waypoints.Count < 10) return;
            Build(track);
            built = true;
        }

        private static void Build(TrackRuntime track)
        {
            var anchor = track.Waypoints[Mathf.Clamp(track.Waypoints.Count - 6, 0, track.Waypoints.Count - 1)];
            var root = new GameObject("Cairo Bridge Gantry").transform;
            root.position = anchor.position;
            root.rotation = anchor.rotation;

            var dark = RuntimeMaterials.Lit(new Color(.012f, .012f, .025f), .15f, .4f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .12f, .82f, 4f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .12f, .82f, 3.6f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .5f, .05f), .2f, .85f, 2.8f);

            Part(root, "Left Tower", new Vector3(-7f, 3.8f, 0f), new Vector3(1.1f, 7.6f, 1.1f), dark);
            Part(root, "Right Tower", new Vector3(7f, 3.8f, 0f), new Vector3(1.1f, 7.6f, 1.1f), dark);
            Part(root, "Bridge Beam", new Vector3(0f, 7.0f, 0f), new Vector3(15f, .8f, 1.1f), dark);
            Part(root, "Purple Rail", new Vector3(0f, 7.6f, -.18f), new Vector3(13.2f, .18f, .22f), purple);
            Part(root, "Cyan Rail", new Vector3(0f, 6.45f, -.18f), new Vector3(13.2f, .18f, .22f), cyan);
            Part(root, "Gold Crest", new Vector3(0f, 8.35f, 0f), new Vector3(4.0f, .25f, .25f), gold);
        }

        private static void Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
