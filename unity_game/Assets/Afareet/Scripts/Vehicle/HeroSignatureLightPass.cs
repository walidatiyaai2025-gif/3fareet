using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public sealed class HeroSignatureLightPass : MonoBehaviour
    {
        private ArcadeCarController car;
        private Light frontLight;
        private Light rearLight;
        private float nitroBlend;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET HERO SIGNATURE LIGHT PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeroSignatureLightPass>();
        }

        private void Update()
        {
            if (car == null)
            {
                var hero = GameObject.Find("PLAYER HERO — AFAREET");
                if (hero == null) return;
                car = hero.GetComponent<ArcadeCarController>();
                if (car == null) return;

                frontLight = AddLight(hero.transform, "Front Cyan Signature", new Vector3(0f, .78f, 2.18f), new Color(.02f, .82f, 1f), 3.8f, 2.7f);
                rearLight = AddLight(hero.transform, "Rear Purple Signature", new Vector3(0f, .52f, -2.12f), new Color(.52f, .02f, 1f), 4.4f, 3.2f);
                AddSilhouetteParts(hero.transform);
            }

            nitroBlend = Mathf.MoveTowards(nitroBlend, car.NitroActive ? 1f : 0f, Time.deltaTime * 4f);
            frontLight.intensity = Mathf.Lerp(2.7f, 4.1f, nitroBlend);
            rearLight.intensity = Mathf.Lerp(3.2f, 5.2f, nitroBlend);
            rearLight.color = Color.Lerp(new Color(.52f, .02f, 1f), new Color(.02f, .82f, 1f), nitroBlend);
        }

        private static void AddSilhouetteParts(Transform hero)
        {
            var purple = RuntimeMaterials.Lit(new Color(.52f, .02f, 1f), .2f, .88f, 3.6f);
            var cyan = RuntimeMaterials.Lit(new Color(.02f, .82f, 1f), .18f, .9f, 3.2f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .25f, .9f, 2.5f);

            AddPart(hero, "Left Spirit Shoulder", new Vector3(-.86f, .92f, -1.54f), new Vector3(.14f, .52f, .78f), purple, Quaternion.Euler(0f, 0f, -13f));
            AddPart(hero, "Right Spirit Shoulder", new Vector3(.86f, .92f, -1.54f), new Vector3(.14f, .52f, .78f), cyan, Quaternion.Euler(0f, 0f, 13f));
            AddPart(hero, "Rear Gold Spine", new Vector3(0f, .88f, -1.52f), new Vector3(.10f, .12f, 1.15f), gold, Quaternion.identity);
        }

        private static void AddPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, Quaternion localRotation)
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

        private static Light AddLight(Transform parent, string name, Vector3 localPosition, Color color, float range, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            return light;
        }
    }
}
