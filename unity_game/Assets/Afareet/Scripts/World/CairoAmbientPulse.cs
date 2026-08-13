using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoAmbientPulse : MonoBehaviour
    {
        private Light[] lights;
        private float[] baseIntensity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var host = new GameObject("Cairo Ambient Pulse");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoAmbientPulse>();
        }

        private void Start()
        {
            lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            baseIntensity = new float[lights.Length];
            for (var i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        }

        private void Update()
        {
            if (lights == null) return;
            var pulse = 1f + Mathf.Sin(Time.time * 1.65f) * .08f;
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null || lights[i].type == LightType.Directional) continue;
                lights[i].intensity = baseIntensity[i] * pulse;
            }
        }
    }
}
