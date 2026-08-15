using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoSkylineSilhouettePass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO SKYLINE SILHOUETTES");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoSkylineSilhouettePass>();
        }

        private void Update()
        {
            if (built) return;
            var a = Waypoint(9);
            var b = Waypoint(33);
            var c = Waypoint(57);
            if (a == null || b == null || c == null) return;

            var dark = RuntimeMaterials.Lit(new Color(.018f, .014f, .035f), .05f, .2f);
            var purple = RuntimeMaterials.Lit(new Color(.42f, .02f, .82f), .08f, .65f, 1.4f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .62f, .85f), .08f, .65f, 1.25f);

            Cluster(a, -1f, 44f, dark, purple);
            Cluster(b, 1f, 50f, dark, cyan);
            Cluster(c, -1f, 48f, dark, purple);
            built = true;
        }

        private static Transform Waypoint(int index)
        {
            var obj = GameObject.Find($"Waypoint {index:00}");
            return obj == null ? null : obj.transform;
        }

        private static void Cluster(Transform anchor, float side, float distance, Material body, Material accent)
        {
            var root = new GameObject("Cairo Skyline Cluster").transform;
            root.position = anchor.position + anchor.right * side * distance;
            root.rotation = anchor.rotation;

            for (var i = -2; i <= 2; i++)
            {
                var h = 10f + (i + 2) * 2.5f;
                Part(root, new Vector3(i * 6f, h * .5f, i % 2 == 0 ? 4f : -3f), new Vector3(4.5f, h, 4.5f), body);
                Part(root, new Vector3(i * 6f, h + .15f, i % 2 == 0 ? 4f : -3f), new Vector3(3.2f, .16f, 3.2f), accent);
            }
        }

        private static void Part(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
