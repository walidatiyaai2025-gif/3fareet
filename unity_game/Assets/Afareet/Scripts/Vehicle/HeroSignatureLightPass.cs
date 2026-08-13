using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class HeroSignatureLightPass : MonoBehaviour
    {
        private bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET HERO SIGNATURE LIGHT PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeroSignatureLightPass>();
        }

        private void Update()
        {
            if (installed) return;
            var hero = GameObject.Find("PLAYER HERO — AFAREET");
            if (hero == null) return;

            AddLight(hero.transform, "Front Cyan Signature", new Vector3(0f, .78f, 2.18f), new Color(.02f, .82f, 1f), 3.8f, 2.7f);
            AddLight(hero.transform, "Rear Purple Signature", new Vector3(0f, .52f, -2.12f), new Color(.52f, .02f, 1f), 4.4f, 3.2f);
            installed = true;
        }

        private static void AddLight(Transform parent, string name, Vector3 localPosition, Color color, float range, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
        }
    }
}
