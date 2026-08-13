using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public sealed class HeroNitroTrailPass : MonoBehaviour
    {
        private ArcadeCarController car;
        private TrailRenderer trail;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET HERO NITRO TRAIL PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeroNitroTrailPass>();
        }

        private void Update()
        {
            if (car == null)
            {
                var hero = GameObject.Find("PLAYER HERO — AFAREET");
                if (hero == null) return;
                car = hero.GetComponent<ArcadeCarController>();
                if (car == null) return;
                trail = CreateTrail(hero.transform);
            }

            var active = car.NitroActive && car.NitroEnergy > 0f;
            var speed = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 160f);
            trail.emitting = active;
            trail.startWidth = Mathf.Lerp(.22f, .38f, speed);
            trail.time = Mathf.Lerp(.18f, .34f, speed);
            trail.startColor = Color.Lerp(new Color(.52f, .02f, 1f, .95f), new Color(.05f, .8f, 1f, 1f), speed);
        }

        private static TrailRenderer CreateTrail(Transform hero)
        {
            var trail = new GameObject("Nitro Spirit Trail").AddComponent<TrailRenderer>();
            trail.transform.SetParent(hero, false);
            trail.transform.localPosition = new Vector3(0f, .4f, -2.05f);
            trail.time = .24f;
            trail.startWidth = .28f;
            trail.endWidth = 0f;
            trail.minVertexDistance = .08f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(.35f, .72f),
                new Keyframe(1f, 0f));
            trail.startColor = new Color(.52f, .02f, 1f, .95f);
            trail.endColor = new Color(.02f, .82f, 1f, 0f);
            trail.material = RuntimeMaterials.Trail(new Color(.22f, .35f, 1f));
            trail.emitting = false;
            return trail;
        }
    }
}
