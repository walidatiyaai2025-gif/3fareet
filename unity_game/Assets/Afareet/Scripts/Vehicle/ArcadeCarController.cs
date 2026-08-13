using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Arcade handling")]
        [SerializeField] private float acceleration = 34f;
        [SerializeField] private float reverseAcceleration = 15f;
        [SerializeField] private float maxSpeed = 48f;
        [SerializeField] private float steerStrength = 105f;
        [SerializeField] private float grip = 8f;
        [SerializeField] private float driftGrip = 2.1f;
        [SerializeField] private float nitroForce = 25f;

        private Rigidbody body;
        private TrailRenderer[] trails;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;

        public bool AcceptsPlayerInput { get; set; }
        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsDrifting => driftInput && Mathf.Abs(steerInput) > 0.15f && Mathf.Abs(SpeedKph) > 25f;
        public bool NitroActive => nitroInput && SpeedKph > 15f;
        public float NitroEnergy { get; private set; } = 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1150f;
            body.linearDamping = 0.15f;
            body.angularDamping = 3.5f;
            body.centerOfMass = new Vector3(0f, -0.45f, 0.15f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            trails = GetComponentsInChildren<TrailRenderer>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (!AcceptsPlayerInput) return;
            throttleInput = Mathf.Clamp(Input.GetAxisRaw("Vertical") + MobileInput.Throttle, -1f, 1f);
            steerInput = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + MobileInput.Steer, -1f, 1f);
            driftInput = Input.GetKey(KeyCode.Space) || MobileInput.Drift;
            nitroInput = Input.GetKey(KeyCode.LeftShift) || MobileInput.Nitro;
            if (Input.GetKeyDown(KeyCode.R)) ResetToSpawn();
        }

        private void FixedUpdate()
        {
            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            var accelerating = throttleInput >= 0f ? acceleration : reverseAcceleration;
            if (Mathf.Abs(forwardSpeed) < maxSpeed || Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed))
                body.AddForce(transform.forward * (throttleInput * accelerating), ForceMode.Acceleration);

            if (NitroActive && NitroEnergy > 0f)
            {
                body.AddForce(transform.forward * nitroForce, ForceMode.Acceleration);
                NitroEnergy = Mathf.Max(0f, NitroEnergy - Time.fixedDeltaTime * 0.22f);
            }
            else NitroEnergy = Mathf.Min(1f, NitroEnergy + Time.fixedDeltaTime * 0.06f);

            var speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 8f);
            var direction = forwardSpeed < -0.5f ? -1f : 1f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, steerInput * steerStrength * speedFactor * direction * Time.fixedDeltaTime, 0f));

            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, (driftInput ? driftGrip : grip) * Time.fixedDeltaTime);
            localVelocity.z = Mathf.Clamp(localVelocity.z, -maxSpeed * 0.3f, maxSpeed * (NitroActive ? 1.22f : 1f));
            body.linearVelocity = transform.TransformDirection(localVelocity);

            foreach (var trail in trails) trail.emitting = IsDrifting || NitroActive;
        }

        public void SetAiInput(float throttle, float steer, bool drift, bool nitro)
        {
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            driftInput = drift;
            nitroInput = nitro;
        }

        public void ResetToSpawn()
        {
            transform.SetPositionAndRotation(spawnPosition + Vector3.up, spawnRotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    public static class MobileInput
    {
        public static float Steer;
        public static float Throttle;
        public static bool Drift;
        public static bool Nitro;
        public static void Reset() { Steer = 0f; Throttle = 0f; Drift = false; Nitro = false; }
    }
}
