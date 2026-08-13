using UnityEngine;

namespace Afareet.World
{
    public sealed class DomeGateLandmarkPass : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("CAIRO DOME GATE LANDMARK PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<DomeGateLandmarkPass>();
        }

        private bool built;

        private void Update()
        {
            if (built) return;
            var track = FindFirstObjectByType<TrackRuntime>();
            if (track == null || track.Waypoints == null || track.Waypoints.Count < 12) return;
            Build(track);
            built = true;
        }

        private static void Build(TrackRuntime track)
        {
            var index = Mathf.Clamp(track.Waypoints.Count / 2, 0, track.Waypoints.Count - 1);
            var anchor = track.Waypoints[index];
            var root = new GameObject("Neon Dome Gate").transform;
            root.position = anchor.position + anchor.right * 24f;
            root.rotation = anchor.rotation;

            var dark = RuntimeMaterials.Lit(new Color(.014f, .012f, .028f), .1f, .35f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .15f, .8f, 4.2f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .15f, .8f, 3.7f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .5f, .05f), .2f, .85f, 2.8f);

            Part(root, "Gate Left", PrimitiveType.Cube, new Vector3(-5.5f, 3.6f, 0f), new Vector3(1.2f, 7.2f, 1.2f), dark);
            Part(root, "Gate Right", PrimitiveType.Cube, new Vector3(5.5f, 3.6f, 0f), new Vector3(1.2f, 7.2f, 1.2f), dark);
            Part(root, "Gate Beam", PrimitiveType.Cube, new Vector3(0f, 7.0f, 0f), new Vector3(12.0f, .7f, 1.2f), gold);

            Part(root, "Dome Base", PrimitiveType.Cylinder, new Vector3(0f, 8.2f, 0f), new Vector3(4.2f, .7f, 4.2f), dark);
            Part(root, "Dome Purple", PrimitiveType.Sphere, new Vector3(0f, 10.1f, 0f), new Vector3(6.8f, 3.7f, 6.8f), purple);
            Part(root, "Dome Cyan Ring", PrimitiveType.Cylinder, new Vector3(0f, 9.2f, 0f), new Vector3(5.1f, .18f, 5.1f), cyan);
            Part(root, "Dome Gold Spire", PrimitiveType.Cylinder, new Vector3(0f, 12.7f, 0f), new Vector3(.3f, 2.2f, .3f), gold);
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
            return obj;
        }
    }
}
