using UnityEngine;

namespace Afareet.World
{
    public sealed class MinaretLandmarkPass : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("CAIRO MINARET LANDMARK PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<MinaretLandmarkPass>();
        }

        private bool built;

        private void Update()
        {
            if (built) return;
            var track = FindFirstObjectByType<TrackRuntime>();
            if (track == null || track.Waypoints == null || track.Waypoints.Count < 8) return;
            Build(track);
            built = true;
        }

        private static void Build(TrackRuntime track)
        {
            var anchor = track.Waypoints[Mathf.Clamp(track.Waypoints.Count / 4, 0, track.Waypoints.Count - 1)];
            var root = new GameObject("Spirit Minaret Cluster").transform;
            root.position = anchor.position - anchor.right * 26f + Vector3.up * .2f;
            root.rotation = anchor.rotation;

            var dark = RuntimeMaterials.Lit(new Color(.018f, .015f, .03f), .1f, .35f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .15f, .8f, 4f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .15f, .8f, 3.5f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .5f, .05f), .2f, .85f, 3f);

            CreateTower(root, new Vector3(-6f, 0f, 0f), 11f, dark, purple, gold);
            CreateTower(root, Vector3.zero, 15f, dark, cyan, gold);
            CreateTower(root, new Vector3(6f, 0f, 0f), 9f, dark, purple, gold);
        }

        private static void CreateTower(Transform parent, Vector3 localPosition, float height, Material body, Material glow, Material gold)
        {
            Part(parent, "Minaret Body", PrimitiveType.Cylinder, localPosition + Vector3.up * height * .5f, new Vector3(1.8f, height * .5f, 1.8f), body);
            Part(parent, "Minaret Balcony", PrimitiveType.Cylinder, localPosition + Vector3.up * (height * .72f), new Vector3(2.6f, .25f, 2.6f), glow);
            Part(parent, "Minaret Crown", PrimitiveType.Cylinder, localPosition + Vector3.up * (height + .7f), new Vector3(1.3f, 1.4f, 1.3f), glow);
            Part(parent, "Minaret Gold Tip", PrimitiveType.Cylinder, localPosition + Vector3.up * (height + 2.1f), new Vector3(.28f, 1.4f, .28f), gold);
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
