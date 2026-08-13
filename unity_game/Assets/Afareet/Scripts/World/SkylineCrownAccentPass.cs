using UnityEngine;

namespace Afareet.World
{
    public sealed class SkylineCrownAccentPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO SKYLINE CROWN ACCENTS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<SkylineCrownAccentPass>();
        }

        private void Update()
        {
            if (built) return;
            var a = Waypoint(9);
            var b = Waypoint(33);
            var c = Waypoint(57);
            if (a == null || b == null || c == null) return;

            var purple = RuntimeMaterials.Lit(new Color(.48f, .025f, .92f), .1f, .8f, 2.2f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .72f, 1f), .1f, .82f, 2f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .04f), .15f, .84f, 2.1f);

            Crown(a, -1f, 44f, 18f, purple, gold, 0);
            Crown(b, 1f, 50f, 20f, cyan, gold, 1);
            Crown(c, -1f, 48f, 17f, purple, cyan, 2);
            built = true;
        }

        private static Transform Waypoint(int index)
        {
            var obj = GameObject.Find($"Waypoint {index:00}");
            return obj == null ? null : obj.transform;
        }

        private static void Crown(Transform anchor, float side, float distance, float height, Material primary, Material secondary, int style)
        {
            var root = new GameObject($"Skyline Crown {style + 1}").transform;
            root.position = anchor.position + anchor.right * side * distance;
            root.rotation = anchor.rotation;

            Part(root, new Vector3(0f, height, 3f), new Vector3(7.5f, .18f, .28f), primary, Quaternion.Euler(0f, 0f, style == 1 ? 0f : 7f));
            Part(root, new Vector3(0f, height + .65f, 3f), new Vector3(4.2f, .14f, .22f), secondary, Quaternion.Euler(0f, 0f, style == 2 ? -8f : 0f));
            Part(root, new Vector3(0f, height + 2.1f, 3f), new Vector3(.28f, 3.1f, .28f), secondary, Quaternion.identity);
        }

        private static void Part(Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion localRotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
