using UnityEngine;

namespace Afareet.World
{
    public sealed class PyramidHorizonLandmarkPass : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("CAIRO PYRAMID HORIZON LANDMARK PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<PyramidHorizonLandmarkPass>();
        }

        private bool built;

        private void Update()
        {
            if (built) return;
            var track = FindFirstObjectByType<TrackRuntime>();
            if (track == null || track.Waypoints == null || track.Waypoints.Count < 16) return;
            Build(track);
            built = true;
        }

        private static void Build(TrackRuntime track)
        {
            var index = Mathf.Clamp(track.Waypoints.Count * 3 / 4, 0, track.Waypoints.Count - 1);
            var anchor = track.Waypoints[index];
            var root = new GameObject("Pyramid Horizon Pair").transform;
            root.position = anchor.position - anchor.right * 40f;
            root.rotation = anchor.rotation;

            var dark = RuntimeMaterials.Lit(new Color(.025f, .018f, .04f), .08f, .28f);
            var purple = RuntimeMaterials.Lit(new Color(.45f, .025f, .9f), .12f, .75f, 1.9f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .05f), .18f, .8f, 2.2f);

            CreatePyramid(root, new Vector3(-9f, 0f, 1.5f), 12f, dark, purple, gold);
            CreatePyramid(root, new Vector3(8f, 0f, -2f), 8.5f, dark, purple, gold);
        }

        private static void CreatePyramid(Transform parent, Vector3 localPosition, float width, Material dark, Material glow, Material gold)
        {
            const int layers = 5;
            for (var layer = 0; layer < layers; layer++)
            {
                var t = layer / (float)layers;
                var size = width * (1f - t * .78f);
                var height = width * .11f;
                var y = height * .5f + layer * height;
                var material = layer == layers - 1 ? glow : dark;
                Part(parent, $"Pyramid Layer {layer + 1}", localPosition + Vector3.up * y, new Vector3(size, height, size), material);
            }
            Part(parent, "Pyramid Gold Apex", localPosition + Vector3.up * (width * .66f), new Vector3(width * .12f, width * .16f, width * .12f), gold);
        }

        private static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
