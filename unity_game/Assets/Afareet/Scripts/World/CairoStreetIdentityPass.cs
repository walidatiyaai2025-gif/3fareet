using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoStreetIdentityPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO STREET IDENTITY PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoStreetIdentityPass>();
        }

        private void Update()
        {
            if (built) return;
            var a = Waypoint(6);
            var b = Waypoint(24);
            var c = Waypoint(42);
            var d = Waypoint(60);
            if (a == null || b == null || c == null || d == null) return;

            var dark = RuntimeMaterials.Lit(new Color(.012f, .012f, .026f), .15f, .42f);
            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .18f, .88f, 3.8f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .9f, 3.4f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .22f, .9f, 3f);

            BuildStreetGate(a, "CAIRO NIGHT", purple, gold, dark, -1f);
            BuildCornerFangs(b, cyan, gold, dark, 1f);
            BuildStreetGate(c, "GIZA SPIRIT", gold, cyan, dark, 1f);
            BuildCornerFangs(d, purple, cyan, dark, -1f);
            built = true;
        }

        private static Transform Waypoint(int index)
        {
            var obj = GameObject.Find($"Waypoint {index:00}");
            return obj == null ? null : obj.transform;
        }

        private static void BuildStreetGate(Transform anchor, string label, Material primary, Material secondary, Material dark, float side)
        {
            var root = new GameObject($"Street Gate // {label}").transform;
            root.position = anchor.position + anchor.right * side * 12.2f;
            root.rotation = anchor.rotation;

            Part(root, "Street Gate Post L", new Vector3(-2.8f, 2.2f, 0f), new Vector3(.35f, 4.4f, .35f), dark, Quaternion.identity);
            Part(root, "Street Gate Post R", new Vector3(2.8f, 2.2f, 0f), new Vector3(.35f, 4.4f, .35f), dark, Quaternion.identity);
            Part(root, "Street Gate Header", new Vector3(0f, 4.1f, 0f), new Vector3(6.0f, .42f, .34f), primary, Quaternion.identity);
            Part(root, "Street Gate Accent", new Vector3(0f, 4.65f, 0f), new Vector3(3.2f, .16f, .22f), secondary, Quaternion.Euler(0f, 0f, side * 8f));
            Part(root, "Street Gate Lower Accent", new Vector3(0f, 3.55f, 0f), new Vector3(4.2f, .10f, .18f), secondary, Quaternion.Euler(0f, 0f, side * -5f));
        }

        private static void BuildCornerFangs(Transform anchor, Material primary, Material secondary, Material dark, float side)
        {
            var root = new GameObject("Cairo Corner Fang Landmark").transform;
            root.position = anchor.position + anchor.right * side * 10.8f;
            root.rotation = anchor.rotation;

            Part(root, "Corner Spine", new Vector3(0f, 1.7f, 0f), new Vector3(.28f, 3.4f, .28f), dark, Quaternion.identity);
            for (var i = 0; i < 3; i++)
            {
                var y = .9f + i * 1.05f;
                var material = i == 1 ? secondary : primary;
                Part(root, "Corner Spirit Fang", new Vector3(side * .62f, y, 0f), new Vector3(.18f, .72f, 1.25f), material, Quaternion.Euler(0f, side * (22f + i * 7f), side * (12f - i * 4f)));
            }
        }

        private static void Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, Quaternion localRotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
