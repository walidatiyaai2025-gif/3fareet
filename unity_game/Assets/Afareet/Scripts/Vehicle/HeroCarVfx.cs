using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class HeroCarVfx : MonoBehaviour
    {
        private ArcadeCarController car;
        private Light nitroGlow;
        private TrailRenderer leftDriftGlow;
        private TrailRenderer rightDriftGlow;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            leftDriftGlow = CreateGlowTrail("Left Drift Glow", new Vector3(-.92f, .2f, -1.32f));
            rightDriftGlow = CreateGlowTrail("Right Drift Glow", new Vector3(.92f, .2f, -1.32f));

            nitroGlow = new GameObject("Nitro Spirit Glow").AddComponent<Light>();
            nitroGlow.transform.SetParent(transform, false);
            nitroGlow.transform.localPosition = new Vector3(0f, .42f, -1.85f);
            nitroGlow.type = LightType.Point;
            nitroGlow.color = new Color(.52f, .02f, 1f);
            nitroGlow.range = 7f;
            nitroGlow.intensity = 0f;
        }

        private void Update()
        {
            if (car == null) return;
            leftDriftGlow.emitting = car.IsDrifting;
            rightDriftGlow.emitting = car.IsDrifting;
            var nitro = car.NitroActive && car.NitroEnergy > 0f;
            nitroGlow.intensity = Mathf.MoveTowards(nitroGlow.intensity, nitro ? 8f : 0f, Time.deltaTime * 18f);
            nitroGlow.color = Color.Lerp(new Color(.52f, .02f, 1f), new Color(.05f, .8f, 1f), 1f - car.NitroEnergy);
        }

        private TrailRenderer CreateGlowTrail(string name, Vector3 localPosition)
        {
            var trail = new GameObject(name).AddComponent<TrailRenderer>();
            trail.transform.SetParent(transform, false);
            trail.transform.localPosition = localPosition;
            trail.time = .24f;
            trail.startWidth = .13f;
            trail.endWidth = 0f;
            trail.startColor = new Color(1f, .55f, .05f, 1f);
            trail.endColor = new Color(.52f, .02f, 1f, 0f);
            trail.emitting = false;
            return trail;
        }
    }
}
