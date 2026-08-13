using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public sealed class HeroFasciaDetailPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET HERO FASCIA DETAIL PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeroFasciaDetailPass>();
        }

        private void Update()
        {
            if (built) return;
            var hero = GameObject.Find("PLAYER HERO — AFAREET");
            if (hero == null) return;

            var cyan = RuntimeMaterials.Lit(new Color(.02f, .82f, 1f), .18f, .9f, 3.2f);
            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .2f, .88f, 3.6f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .25f, .9f, 2.5f);
            var dark = RuntimeMaterials.Lit(new Color(.01f, .012f, .025f), .45f, .72f);

            Part(hero.transform, "Left Spirit Brow", PrimitiveType.Cube, new Vector3(-.58f, .79f, 2.24f), new Vector3(.62f, .075f, .10f), cyan, Quaternion.Euler(0f, 0f, -8f));
            Part(hero.transform, "Right Spirit Brow", PrimitiveType.Cube, new Vector3(.58f, .79f, 2.24f), new Vector3(.62f, .075f, .10f), cyan, Quaternion.Euler(0f, 0f, 8f));
            Part(hero.transform, "Gold Chin Blade", PrimitiveType.Cube, new Vector3(0f, .27f, 2.31f), new Vector3(1.55f, .07f, .12f), gold, Quaternion.identity);
            Part(hero.transform, "Front Dark Intake Bar", PrimitiveType.Cube, new Vector3(0f, .48f, 2.29f), new Vector3(1.15f, .18f, .08f), dark, Quaternion.identity);

            Part(hero.transform, "Left Exhaust Spirit Halo", PrimitiveType.Cylinder, new Vector3(-.62f, .29f, -2.25f), new Vector3(.27f, .055f, .27f), purple, Quaternion.Euler(90f, 0f, 0f));
            Part(hero.transform, "Right Exhaust Spirit Halo", PrimitiveType.Cylinder, new Vector3(.62f, .29f, -2.25f), new Vector3(.27f, .055f, .27f), cyan, Quaternion.Euler(90f, 0f, 0f));
            Part(hero.transform, "Rear Diffuser Fang L", PrimitiveType.Cube, new Vector3(-.35f, .21f, -2.18f), new Vector3(.10f, .24f, .22f), gold, Quaternion.Euler(0f, 0f, -16f));
            Part(hero.transform, "Rear Diffuser Fang R", PrimitiveType.Cube, new Vector3(.35f, .21f, -2.18f), new Vector3(.10f, .24f, .22f), gold, Quaternion.Euler(0f, 0f, 16f));
            built = true;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
        }
    }
}
