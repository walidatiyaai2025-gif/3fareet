using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class CrashResponsePass : MonoBehaviour
    {
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<CrashResponsePass>() != null) return;
            var host = new GameObject("AFAREET CRASH RESPONSE PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CrashResponsePass>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;

            var cars = FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None);
            foreach (var car in cars)
            {
                if (car.GetComponent<CrashResponseRelay>() == null)
                    car.gameObject.AddComponent<CrashResponseRelay>();
            }
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrashResponseRelay : MonoBehaviour
    {
        private Rigidbody body;
        private float lastImpactAt = -10f;

        public event Action<float, Vector3> Impacted;
        public float LastImpactSpeed { get; private set; }

        private void Awake() => body = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision collision)
        {
            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < 7f || Time.time - lastImpactAt < .08f) return;

            lastImpactAt = Time.time;
            LastImpactSpeed = impactSpeed;

            var contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : -transform.forward;
            var planar = Vector3.ProjectOnPlane(body.linearVelocity, contactNormal);
            var retention = Mathf.Lerp(.86f, .64f, Mathf.InverseLerp(7f, 32f, impactSpeed));
            body.linearVelocity = planar * retention;
            body.angularVelocity *= Mathf.Lerp(.72f, .42f, Mathf.InverseLerp(7f, 32f, impactSpeed));

            Impacted?.Invoke(impactSpeed, collision.contactCount > 0 ? collision.GetContact(0).point : transform.position);
        }
    }
}
