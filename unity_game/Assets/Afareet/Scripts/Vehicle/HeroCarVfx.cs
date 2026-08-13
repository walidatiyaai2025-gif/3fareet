using UnityEngine;
using Afareet.World;

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
            var drifting = car.IsDrifting;
            leftDriftGlow.emitting = drifting;
            rightDriftGlow.emitting = drifting;

            var driftPower = drifting ? Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 150f) : 0f;
            var width = Mathf.Lerp(.13f, .23f, driftPower);
            var life = Mathf.Lerp(.24f, .42f, driftPower);
            leftDriftGlow.startWidth = width;
            rightDriftGlow.startWidth = width;
            leftDriftGlow.time = life;
            rightDriftGlow.time = life;

            var nitro = car.NitroActive && car.NitroEnergy > 0f;
            nitroGlow.intensity = Mathf.MoveTowards(nitroGlow.intensity, nitro ? 8f : 0f, Time.deltaTime * 18f);
            nitroGlow.color = Color.Lerp(new Color(.52f, .02f, 1f), new Color(.05f, .8f, 1f), 1f - car.NitroEnergy);
        }

        private TrailRenderer CreateGlowTrail(string name, Vector3 localPosition)
        {
            var trail = new GameObject(name).AddComponent<TrailRenderer>();
            trail.transform.SetParent(transform, false);
            trail.transform.localPosition = localPosition;
            trail.time = .34f;
            trail.startWidth = .19f;
            trail.endWidth = 0f;
            trail.minVertexDistance = .08f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(.28f, .92f),
                new Keyframe(.68f, .48f),
                new Keyframe(1f, 0f));
            trail.startColor = new Color(1f, .55f, .05f, 1f);
            trail.endColor = new Color(.52f, .02f, 1f, 0f);
            trail.material = RuntimeMaterials.Trail(new Color(.72f, .08f, 1f));
            trail.emitting = false;
            return trail;
        }
    }
}
