using UnityEngine;

namespace Afareet.World
{
    public sealed class CornerChevronIdentityPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO CORNER CHEVRON IDENTITY PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CornerChevronIdentityPass>();
        }

        private void Update()
        {
            if (built) return;
            var a = Waypoint(14);
            var b = Waypoint(31);
            var c = Waypoint(49);
            var d = Waypoint(67);
            if (a == null || b == null || c == null || d == null) return;

            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .18f, .9f, 4f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .9f, 3.6f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .22f, .9f, 3f);

            Cluster(a, -1f, purple, gold);
            Cluster(b, 1f, cyan, gold);
            Cluster(c, -1f, gold, purple);
            Cluster(d, 1f, purple, cyan);
            built = true;
        }

        private static Transform Waypoint(int index)
        {
            var obj = GameObject.Find($"Waypoint {index:00}");
            return obj == null ? null : obj.transform;
        }

        private static void Cluster(Transform wp, float side, Material primary, Material accent)
        {
            var root = new GameObject("Corner Spirit Chevron Cluster").transform;
            root.position = wp.position + wp.right * side * 9.7f;
            root.rotation = wp.rotation;

            for (var i = 0; i < 3; i++)
            {
                var z = (i - 1) * 2.1f;
                Chevron(root, new Vector3(0f, 1.2f, z), side, i == 1 ? accent : primary);
            }
        }

        private static void Chevron(Transform parent, Vector3 localPosition, float side, Material material)
        {
            Part(parent, localPosition + new Vector3(-.34f * side, .22f, 0f), new Vector3(.16f, 1.05f, .16f), material, Quaternion.Euler(0f, side * 34f, side * 34f));
            Part(parent, localPosition + new Vector3(.34f * side, -.22f, 0f), new Vector3(.16f, 1.05f, .16f), material, Quaternion.Euler(0f, side * 34f, side * -34f));
        }

        private static void Part(Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion localRotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Spirit Corner Chevron";
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
