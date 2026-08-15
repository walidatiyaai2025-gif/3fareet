using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public sealed class HeroPaintContrastPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET HERO PAINT CONTRAST PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeroPaintContrastPass>();
        }

        private void Update()
        {
            if (built) return;
            var hero = GameObject.Find("PLAYER HERO — AFAREET");
            if (hero == null) return;

            var matte = RuntimeMaterials.Lit(new Color(.006f, .007f, .014f), .25f, .38f);
            var purple = RuntimeMaterials.Lit(new Color(.34f, .015f, .66f), .45f, .72f, 1.2f);
            var gold = RuntimeMaterials.Lit(new Color(1f, .46f, .03f), .62f, .92f, 1.6f);

            Part(hero.transform, "Matte Hood Center", new Vector3(0f, .91f, 1.03f), new Vector3(.72f, .018f, 1.52f), matte, Quaternion.identity);
            Part(hero.transform, "Matte Roof Center", new Vector3(0f, 1.39f, -.32f), new Vector3(.74f, .018f, .86f), matte, Quaternion.identity);
            Part(hero.transform, "Left Purple Shoulder Paint", new Vector3(-.81f, .82f, .30f), new Vector3(.13f, .022f, 1.65f), purple, Quaternion.Euler(0f, -7f, 0f));
            Part(hero.transform, "Right Purple Shoulder Paint", new Vector3(.81f, .82f, .30f), new Vector3(.13f, .022f, 1.65f), purple, Quaternion.Euler(0f, 7f, 0f));
            Part(hero.transform, "Gold Roof Micro Stripe", new Vector3(0f, 1.42f, -.32f), new Vector3(.08f, .022f, 1.08f), gold, Quaternion.identity);
            built = true;
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
