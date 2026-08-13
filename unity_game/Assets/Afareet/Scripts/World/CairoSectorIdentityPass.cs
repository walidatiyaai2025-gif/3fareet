using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoSectorIdentityPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("CAIRO SECTOR IDENTITY PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoSectorIdentityPass>();
        }

        private void Update()
        {
            if (built) return;
            var track = FindFirstObjectByType<TrackRuntime>();
            if (track == null || track.Waypoints == null || track.Waypoints.Count < 55) return;

            BuildBeacon(track.Waypoints[0], "ROYAL START", new Color(1f, .48f, .035f), new Color(.52f, .02f, 1f));
            BuildBeacon(track.Waypoints[18], "NEON CITY", new Color(.02f, .78f, 1f), new Color(.52f, .02f, 1f));
            BuildBeacon(track.Waypoints[36], "GIZA GOLD", new Color(1f, .48f, .035f), new Color(.02f, .78f, 1f));
            BuildBeacon(track.Waypoints[54], "SPIRIT RETURN", new Color(.52f, .02f, 1f), new Color(.02f, .78f, 1f));
            built = true;
        }

        private static void BuildBeacon(Transform anchor, string label, Color primaryColor, Color secondaryColor)
        {
            var root = new GameObject($"Sector Beacon // {label}").transform;
            root.position = anchor.position + anchor.right * 18f;
            root.rotation = anchor.rotation;

            var dark = RuntimeMaterials.Lit(new Color(.015f, .012f, .03f), .15f, .45f);
            var primary = RuntimeMaterials.Lit(primaryColor, .2f, .88f, 3.8f);
            var secondary = RuntimeMaterials.Lit(secondaryColor, .18f, .9f, 3.4f);

            Part(root, "Sector Mast", new Vector3(0f, 2.4f, 0f), new Vector3(.32f, 4.8f, .32f), dark, Quaternion.identity);
            Part(root, "Primary Blade", new Vector3(-.55f, 4.0f, 0f), new Vector3(.16f, 2.4f, .42f), primary, Quaternion.Euler(0f, 0f, -11f));
            Part(root, "Secondary Blade", new Vector3(.55f, 4.0f, 0f), new Vector3(.16f, 2.4f, .42f), secondary, Quaternion.Euler(0f, 0f, 11f));
            Part(root, "Sector Crown", new Vector3(0f, 5.35f, 0f), new Vector3(2.2f, .14f, .34f), primary, Quaternion.identity);
        }

        private static void Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, Quaternion localRotation)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
        }
    }
}
