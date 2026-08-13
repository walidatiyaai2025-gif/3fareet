using UnityEngine;

namespace Afareet.World
{
    public sealed class RoadsideArtPass : MonoBehaviour
    {
        private static Material dark;
        private static Material gold;
        private static Material purple;
        private static Material cyan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET ROADSIDE ART PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<RoadsideArtPass>();
        }

        private void Start()
        {
            dark = RuntimeMaterials.Lit(new Color(.018f, .012f, .03f), .25f, .55f);
            gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .2f, .86f, 3.5f);
            purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .18f, .88f, 4.4f);
            cyan = RuntimeMaterials.Lit(new Color(.02f, .74f, 1f), .15f, .9f, 3.8f);
            InvokeRepeating(nameof(TryBuild), .35f, .35f);
        }

        private void TryBuild()
        {
            if (GameObject.Find("AFAREET ROADSIDE ART") != null) { CancelInvoke(); return; }
            var first = GameObject.Find("Waypoint 00");
            if (first == null) return;

            var root = new GameObject("AFAREET ROADSIDE ART").transform;
            for (var i = 0; i < 72; i += 6)
            {
                var wp = GameObject.Find($"Waypoint {i:00}");
                if (wp == null) continue;
                BuildMarker(root, wp.transform, -1f, i);
                BuildMarker(root, wp.transform, 1f, i);
            }
            BuildFinishHero(root, first.transform);
            CancelInvoke();
        }

        private static void BuildMarker(Transform root, Transform wp, float side, int seed)
        {
            var p = wp.position + wp.right * side * 9.2f;
            var pole = Cube(root, "Roadside Spirit Pole", p + Vector3.up * 1.5f, new Vector3(.18f, 3f, .18f), dark, wp.rotation);
            _ = pole;
            var bladeMat = seed % 12 == 0 ? gold : (side < 0 ? purple : cyan);
            Cube(root, "Roadside Neon Blade", p + Vector3.up * 2.55f, new Vector3(.18f, 1.25f, 1.15f), bladeMat, wp.rotation * Quaternion.Euler(0f, side * 12f, side * 8f));
            Cube(root, "Roadside Hazard Foot", p + Vector3.up * .08f, new Vector3(.9f, .16f, .9f), seed % 18 == 0 ? gold : purple, wp.rotation * Quaternion.Euler(0f, 45f, 0f));
        }

        private static void BuildFinishHero(Transform root, Transform start)
        {
            var center = start.position + Vector3.up * 8.1f;
            Cube(root, "Finish Crown Gold", center, new Vector3(5.4f, .18f, .22f), gold, start.rotation * Quaternion.Euler(0f, 0f, 12f));
            Cube(root, "Finish Crown Purple", center, new Vector3(5.4f, .18f, .22f), purple, start.rotation * Quaternion.Euler(0f, 0f, -12f));
            Cube(root, "Finish Crown Cyan", center + Vector3.up * .55f, new Vector3(2.8f, .13f, .18f), cyan, start.rotation);
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name = name;
            Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent);
            o.transform.SetPositionAndRotation(position, rotation);
            o.transform.localScale = scale;
            o.GetComponent<Renderer>().material = material;
            return o;
        }
    }
}
