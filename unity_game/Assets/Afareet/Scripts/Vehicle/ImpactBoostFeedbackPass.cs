using Afareet.World;
using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class ImpactBoostFeedbackPass : MonoBehaviour
    {
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<ImpactBoostFeedbackPass>() != null) return;
            var host = new GameObject("AFAREET IMPACT BOOST FEEDBACK PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<ImpactBoostFeedbackPass>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;

            foreach (var car in FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None))
            {
                if (car.GetComponent<VehicleFeedbackVfx>() == null)
                    car.gameObject.AddComponent<VehicleFeedbackVfx>();
            }
        }
    }

    public sealed class VehicleFeedbackVfx : MonoBehaviour
    {
        private ArcadeCarController car;
        private CrashResponseRelay crash;
        private Renderer impactFlash;
        private Renderer boostFlash;
        private float impactTimer;
        private float boostTimer;
        private bool nitroWasActive;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            impactFlash = CreateFlash(
                "Collision Spirit Flash",
                RuntimeMaterials.Lit(new Color(1f, .48f, .035f), .2f, .9f, 3f),
                Vector3.zero);
            boostFlash = CreateFlash(
                "Boost Spirit Flash",
                RuntimeMaterials.Lit(new Color(.02f, .78f, 1f), .16f, .9f, 3.4f),
                new Vector3(0f, .48f, -2.12f));
            boostFlash.transform.localScale = new Vector3(.65f, .65f, .18f);
        }

        private void OnEnable() => TryBindCrash();

        private void OnDisable()
        {
            if (crash != null) crash.Impacted -= OnImpact;
        }

        private void Update()
        {
            TryBindCrash();
            if (car == null) return;

            var nitroActive = car.NitroActive && car.NitroEnergy > 0f;
            if (nitroActive && !nitroWasActive) TriggerBoostPickup();
            nitroWasActive = nitroActive;

            if (impactTimer > 0f)
            {
                impactTimer -= Time.deltaTime;
                var t = Mathf.Clamp01(impactTimer / .2f);
                impactFlash.enabled = true;
                impactFlash.transform.localScale = Vector3.one * Mathf.Lerp(1.55f, .35f, 1f - t);
                if (impactTimer <= 0f) impactFlash.enabled = false;
            }

            if (boostTimer > 0f)
            {
                boostTimer -= Time.deltaTime;
                var t = Mathf.Clamp01(boostTimer / .28f);
                boostFlash.enabled = true;
                boostFlash.transform.localScale = new Vector3(
                    Mathf.Lerp(1.55f, .65f, 1f - t),
                    Mathf.Lerp(1.55f, .65f, 1f - t),
                    .18f);
                if (boostTimer <= 0f) boostFlash.enabled = false;
            }
        }

        public void TriggerBoostPickup() => boostTimer = .28f;

        private void TryBindCrash()
        {
            if (crash != null) return;
            crash = GetComponent<CrashResponseRelay>();
            if (crash != null) crash.Impacted += OnImpact;
        }

        private void OnImpact(float speed, Vector3 worldPoint)
        {
            impactFlash.transform.position = worldPoint;
            impactFlash.transform.localScale = Vector3.one * Mathf.Lerp(.55f, 1.55f, Mathf.InverseLerp(7f, 32f, speed));
            impactTimer = .2f;
        }

        private Renderer CreateFlash(string name, Material material, Vector3 localPosition)
        {
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = name;
            Destroy(flash.GetComponent<Collider>());
            flash.transform.SetParent(transform, false);
            flash.transform.localPosition = localPosition;
            flash.transform.localScale = Vector3.one * .4f;
            var renderer = flash.GetComponent<Renderer>();
            renderer.material = material;
            renderer.enabled = false;
            return renderer;
        }
    }
}
