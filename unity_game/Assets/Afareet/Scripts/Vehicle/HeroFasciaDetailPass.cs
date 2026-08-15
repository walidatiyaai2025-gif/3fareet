using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public sealed class HeroFasciaDetailPass : MonoBehaviour
    {
        private ArcadeCarController car;
        private Transform leftHalo;
        private Transform rightHalo;
        private Renderer leftHaloRenderer;
        private Renderer rightHaloRenderer;
        private Vector3 haloBaseScale;
        private float nitroBlend;
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
            if (!built)
            {
                var hero = GameObject.Find("PLAYER HERO — AFAREET");
                if (hero == null) return;
                car = hero.GetComponent<ArcadeCarController>();
                if (car == null) return;
                Build(hero.transform);
                built = true;
            }

            if (leftHalo == null || rightHalo == null || car == null) return;
            var active = car.NitroActive && car.NitroEnergy > 0f;
            nitroBlend = Mathf.MoveTowards(nitroBlend, active ? 1f : 0f, Time.deltaTime * 5f);
            var speed = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 180f);
            var pulse = active ? .5f + .5f * Mathf.Sin(Time.time * 18f) : 0f;
            var scale = 1f + nitroBlend * (.28f + speed * .20f + pulse * .10f);
            leftHalo.localScale = haloBaseScale * scale;
            rightHalo.localScale = haloBaseScale * scale;

            var purple = Color.Lerp(new Color(.52f, .02f, 1f), new Color(.02f, .82f, 1f), nitroBlend * .8f);
            var cyan = Color.Lerp(new Color(.02f, .82f, 1f), new Color(1f, .48f, .035f), nitroBlend * .65f);
            leftHaloRenderer.material.SetColor("_Color", purple);
            rightHaloRenderer.material.SetColor("_Color", cyan);
            leftHaloRenderer.material.SetColor("_EmissionColor", purple * Mathf.Lerp(2.2f, 5f, nitroBlend));
            rightHaloRenderer.material.SetColor("_EmissionColor", cyan * Mathf.Lerp(2.2f, 5f, nitroBlend));
        }

        private void Build(Transform hero)
        {
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .82f, 1f), .18f, .9f, 3.2f);
            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .2f, .88f, 3.6f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .25f, .9f, 2.5f);
            var dark = RuntimeMaterials.Lit(new Color(.01f, .012f, .025f), .45f, .72f);

            Part(hero, "Left Spirit Brow", PrimitiveType.Cube, new Vector3(-.58f, .79f, 2.24f), new Vector3(.62f, .075f, .10f), cyan, Quaternion.Euler(0f, 0f, -8f));
            Part(hero, "Right Spirit Brow", PrimitiveType.Cube, new Vector3(.58f, .79f, 2.24f), new Vector3(.62f, .075f, .10f), cyan, Quaternion.Euler(0f, 0f, 8f));
            Part(hero, "Gold Chin Blade", PrimitiveType.Cube, new Vector3(0f, .27f, 2.31f), new Vector3(1.55f, .07f, .12f), gold, Quaternion.identity);
            Part(hero, "Front Dark Intake Bar", PrimitiveType.Cube, new Vector3(0f, .48f, 2.29f), new Vector3(1.15f, .18f, .08f), dark, Quaternion.identity);

            leftHalo = Part(hero, "Left Exhaust Spirit Halo", PrimitiveType.Cylinder, new Vector3(-.62f, .29f, -2.25f), new Vector3(.27f, .055f, .27f), purple, Quaternion.Euler(90f, 0f, 0f)).transform;
            rightHalo = Part(hero, "Right Exhaust Spirit Halo", PrimitiveType.Cylinder, new Vector3(.62f, .29f, -2.25f), new Vector3(.27f, .055f, .27f), cyan, Quaternion.Euler(90f, 0f, 0f)).transform;
            haloBaseScale = leftHalo.localScale;
            leftHaloRenderer = leftHalo.GetComponent<Renderer>();
            rightHaloRenderer = rightHalo.GetComponent<Renderer>();

            Part(hero, "Rear Diffuser Fang L", PrimitiveType.Cube, new Vector3(-.35f, .21f, -2.18f), new Vector3(.10f, .24f, .22f), gold, Quaternion.Euler(0f, 0f, -16f));
            Part(hero, "Rear Diffuser Fang R", PrimitiveType.Cube, new Vector3(.35f, .21f, -2.18f), new Vector3(.10f, .24f, .22f), gold, Quaternion.Euler(0f, 0f, 16f));
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
            return obj;
        }
    }
}
