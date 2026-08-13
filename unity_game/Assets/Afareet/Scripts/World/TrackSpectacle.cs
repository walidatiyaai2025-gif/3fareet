using UnityEngine;

namespace Afareet.World
{
    public sealed class TrackSpectacle : MonoBehaviour
    {
        public void Build(TrackRuntime track)
        {
            if (track == null || track.Waypoints.Count == 0) return;
            var root = new GameObject("3FAREET TRACK SPECTACLE").transform;
            root.SetParent(transform, false);
            var cyan = RuntimeMaterials.Lit(new Color(0f, .72f, 1f), .2f, .85f, 4.2f);
            var purple = RuntimeMaterials.Lit(new Color(.5f, .03f, .95f), .2f, .85f, 4.8f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .06f), .2f, .85f, 3.5f);
            var dark = RuntimeMaterials.Lit(new Color(.018f, .012f, .035f), .2f, .4f);

            for (var i = 0; i < track.Waypoints.Count; i += 12)
                CreateBanner(root, track.Waypoints[i], i, dark, i % 24 == 0 ? gold : purple);

            for (var i = 6; i < track.Waypoints.Count; i += 18)
                CreateArch(root, track.Waypoints[i], cyan, purple, gold);

            CreateStartCrown(root, track.Waypoints[0], cyan, purple, gold, dark);
        }

        private static void CreateBanner(Transform root, Transform anchor, int seed, Material frame, Material glow)
        {
            var banner = new GameObject($"3Fareet Track Banner {seed:00}").transform;
            banner.SetParent(root);
            banner.SetPositionAndRotation(anchor.position - anchor.right * 18f + Vector3.up * 2.2f, anchor.rotation);
            Part(banner, "Frame", Vector3.zero, new Vector3(3.8f, 1.7f, .18f), frame, Quaternion.identity);
            Part(banner, "Slash A", new Vector3(-.65f, 0f, -.11f), new Vector3(.22f, 1.1f, .05f), glow, Quaternion.Euler(0f, 0f, 24f));
            Part(banner, "Slash B", new Vector3(.1f, 0f, -.11f), new Vector3(.22f, 1.1f, .05f), glow, Quaternion.Euler(0f, 0f, -24f));
            Part(banner, "Slash C", new Vector3(.85f, 0f, -.11f), new Vector3(.22f, 1.1f, .05f), glow, Quaternion.Euler(0f, 0f, 24f));
        }

        private static void CreateArch(Transform root, Transform anchor, Material cyan, Material purple, Material gold)
        {
            Part(root, "Spirit Arch Left", anchor.position - anchor.right * 6.5f + Vector3.up * 2.6f, new Vector3(.28f, 5.2f, .28f), purple, anchor.rotation, false);
            Part(root, "Spirit Arch Right", anchor.position + anchor.right * 6.5f + Vector3.up * 2.6f, new Vector3(.28f, 5.2f, .28f), cyan, anchor.rotation, false);
            Part(root, "Spirit Arch Beam", anchor.position + Vector3.up * 5.2f, new Vector3(13.2f, .28f, .28f), gold, anchor.rotation, false);
        }

        private static void CreateStartCrown(Transform root, Transform start, Material cyan, Material purple, Material gold, Material dark)
        {
            var back = -start.forward * 7f;
            for (var side = -1; side <= 1; side += 2)
            {
                var basePos = start.position + back + start.right * side * 9.2f;
                Part(root, "Start Totem", basePos + Vector3.up * 2.4f, new Vector3(.6f, 4.8f, .6f), dark, start.rotation, false);
                Part(root, "Start Totem Glow", basePos + Vector3.up * 4.2f, new Vector3(1.1f, 1.6f, .3f), side < 0 ? purple : cyan, start.rotation, false);
            }
            Part(root, "Start Crown", start.position + back + Vector3.up * 5.8f, new Vector3(8.2f, .25f, .35f), gold, start.rotation, false);
            Part(root, "Start Crown Spirit", start.position + back + Vector3.up * 6.35f, new Vector3(4.5f, .16f, .24f), purple, start.rotation * Quaternion.Euler(0f, 0f, 6f), false);
        }

        private static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation, bool local = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            if (local) obj.transform.localPosition = position; else obj.transform.position = position;
            obj.transform.rotation = local ? parent.rotation * rotation : rotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
            return obj;
        }
    }
}
