using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private ArcadeCarConfig config;

        private Rigidbody body;
        private TrailRenderer[] trails;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;
        private bool brakeInput;

        public bool AcceptsPlayerInput { get; set; }
        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsDrifting => driftInput && Mathf.Abs(steerInput) > 0.15f && Mathf.Abs(SpeedKph) > 25f;
        public bool NitroActive => nitroInput && SpeedKph > 15f;
        public float NitroEnergy { get; private set; } = 1f;
        public float CurrentSteerInput => steerInput;
        public float CurrentThrottleInput => throttleInput;
        public bool CurrentBrakeInput => brakeInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            trails = GetComponentsInChildren<TrailRenderer>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetToSpawn();
        }

        private void FixedUpdate()
        {
            if (config == null) return;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (AcceptsPlayerInput) ReadDesktopInput();
#endif
            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            if (brakeInput && forwardSpeed > .25f)
            {
                localVelocity.z = Mathf.MoveTowards(localVelocity.z, 0f, 34f * Time.fixedDeltaTime);
                body.linearVelocity = transform.TransformDirection(localVelocity);
                forwardSpeed = localVelocity.z;
            }
            var accelerating = throttleInput >= 0f ? config.acceleration : config.reverseAcceleration;
            if (Mathf.Abs(forwardSpeed) < config.maxSpeedMetersPerSecond || Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed))
                body.AddForce(transform.forward * (throttleInput * accelerating), ForceMode.Acceleration);

            if (NitroActive && NitroEnergy > 0f)
            {
                body.AddForce(transform.forward * config.nitroForce, ForceMode.Acceleration);
                NitroEnergy = Mathf.Max(0f, NitroEnergy - Time.fixedDeltaTime * config.nitroConsumptionPerSecond);
            }
            else NitroEnergy = Mathf.Min(1f, NitroEnergy + Time.fixedDeltaTime * config.nitroRechargePerSecond);

            var speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 8f);
            var direction = forwardSpeed < -0.5f ? -1f : 1f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, steerInput * config.steerStrengthDegrees * speedFactor * direction * Time.fixedDeltaTime, 0f));

            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, (driftInput ? config.driftGrip : config.grip) * Time.fixedDeltaTime);
            localVelocity.z = Mathf.Clamp(localVelocity.z, -config.maxSpeedMetersPerSecond * 0.3f, config.maxSpeedMetersPerSecond * (NitroActive ? 1.22f : 1f));
            body.linearVelocity = transform.TransformDirection(localVelocity);

            foreach (var trail in trails) trail.emitting = IsDrifting || NitroActive;
        }

        private void ReadDesktopInput()
        {
            throttleInput = Input.GetAxisRaw("Vertical");
            steerInput = Input.GetAxisRaw("Horizontal");
            driftInput = Input.GetKey(KeyCode.Space);
            nitroInput = Input.GetKey(KeyCode.LeftShift);
            brakeInput = Input.GetKey(KeyCode.DownArrow);
        }

        public void SetPlayerInput(float throttle, float steer, bool drift, bool nitro, bool brake)
        {
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            driftInput = drift;
            nitroInput = nitro;
            brakeInput = brake;
        }

        public void Configure(ArcadeCarConfig vehicleConfig)
        {
            if (vehicleConfig == null) throw new System.ArgumentNullException(nameof(vehicleConfig));
            if (!vehicleConfig.IsValid(out var error))
                throw new System.ArgumentException(error, nameof(vehicleConfig));

            config = vehicleConfig;
            body.mass = config.massKilograms;
            body.linearDamping = config.linearDamping;
            body.angularDamping = config.angularDamping;
            body.centerOfMass = config.centerOfMass;
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

}
