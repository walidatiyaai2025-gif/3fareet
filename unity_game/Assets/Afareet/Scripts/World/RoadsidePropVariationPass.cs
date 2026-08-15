using UnityEngine;

namespace Afareet.World
{
    public sealed class RoadsidePropVariationPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO ROADSIDE PROP VARIATION PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<RoadsidePropVariationPass>();
        }

        private void Update()
        {
            if (built) return;
            if (GameObject.Find("Waypoint 60") == null) return;

            var dark = RuntimeMaterials.Lit(new Color(.016f, .013f, .03f), .18f, .45f);
            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .16f, .88f, 3.5f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .9f, 3.2f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .2f, .9f, 2.8f);

            for (var i = 6; i < 72; i += 12)
            {
                var wp = GameObject.Find($"Waypoint {i:00}");
                if (wp == null) continue;
                var side = ((i / 12) % 2 == 0) ? -1f : 1f;
                var mode = (i / 6) % 3;
                if (mode == 0) BuildSpiritPylon(wp.transform, side, dark, purple, cyan);
                else if (mode == 1) BuildBannerFrame(wp.transform, side, dark, gold, purple);
                else BuildGoldFangs(wp.transform, side, dark, gold, cyan);
            }

            built = true;
        }

        private static void BuildSpiritPylon(Transform wp, float side, Material dark, Material purple, Material cyan)
        {
            var p = wp.position + wp.right * side * 12f;
            Part("Spirit Pylon Body", p + Vector3.up * 2.1f, new Vector3(.7f, 4.2f, .7f), dark, wp.rotation);
            Part("Spirit Pylon Purple Blade", p + Vector3.up * 4.0f, new Vector3(.22f, 1.4f, 1.6f), purple, wp.rotation * Quaternion.Euler(0f, side * 18f, side * 8f));
            Part("Spirit Pylon Cyan Crown", p + Vector3.up * 5.0f, new Vector3(1.4f, .16f, 1.4f), cyan, wp.rotation * Quaternion.Euler(0f, 45f, 0f));
        }

        private static void BuildBannerFrame(Transform wp, float side, Material dark, Material gold, Material purple)
        {
            var p = wp.position + wp.right * side * 12.5f;
            Part("Cairo Banner Post", p + Vector3.up * 2.0f, new Vector3(.35f, 4f, .35f), dark, wp.rotation);
            Part("Cairo Banner Gold Arm", p + Vector3.up * 3.8f + wp.forward * .8f, new Vector3(.22f, .18f, 2.0f), gold, wp.rotation);
            Part("Cairo Banner Spirit Plate", p + Vector3.up * 2.8f + wp.forward * 1.6f, new Vector3(1.8f, 1.5f, .10f), purple, wp.rotation);
        }

        private static void BuildGoldFangs(Transform wp, float side, Material dark, Material gold, Material cyan)
        {
            var p = wp.position + wp.right * side * 11.8f;
            Part("Fang Base", p + Vector3.up * .2f, new Vector3(2.0f, .4f, 1.5f), dark, wp.rotation);
            Part("Gold Fang A", p + Vector3.up * 1.6f - wp.right * .45f, new Vector3(.24f, 3f, .5f), gold, wp.rotation * Quaternion.Euler(0f, 0f, side * 14f));
            Part("Gold Fang B", p + Vector3.up * 1.35f + wp.right * .45f, new Vector3(.20f, 2.5f, .5f), cyan, wp.rotation * Quaternion.Euler(0f, 0f, -side * 12f));
        }

        private static void Part(string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
