using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoAmbientPulse : MonoBehaviour
    {
        private Light[] lights;
        private float[] baseIntensity;
        private float captureDelay = .8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var host = new GameObject("Cairo Ambient Pulse");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CairoAmbientPulse>();
        }

        private void Update()
        {
            if (lights == null)
            {
                captureDelay -= Time.deltaTime;
                if (captureDelay <= 0f) CaptureLights();
                return;
            }

            var pulse = 1f + Mathf.Sin(Time.time * 1.65f) * .08f;
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type == LightType.Directional || IsVehicleLight(light)) continue;
                light.intensity = baseIntensity[i] * pulse;
            }
        }

        private void CaptureLights()
        {
            lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            baseIntensity = new float[lights.Length];
            for (var i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        }

        private static bool IsVehicleLight(Light light)
        {
            var rootName = light.transform.root.name;
            return rootName.Contains("PLAYER HERO") || rootName.StartsWith("RIVAL ");
        }
    }
}
