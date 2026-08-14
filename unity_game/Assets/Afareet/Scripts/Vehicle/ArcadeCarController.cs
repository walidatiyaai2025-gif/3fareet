using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private ArcadeCarConfig config;

        private Rigidbody body;
        private TrailRenderer[] trails;
        private SurfaceResponseProbe surface;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;
        private bool brakeInput;
        private bool nitroEngaged;
        private bool wasDrifting;
        private float nitroCooldownRemaining;
        private float driftReentryRemaining;

        public bool AcceptsPlayerInput { get; set; }
        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsDrifting { get; private set; }
        public bool NitroActive => nitroEngaged;
        public float NitroEnergy { get; private set; } = 1f;
        public float DriftEnergy { get; private set; }
        public float NitroCooldownRemaining => nitroCooldownRemaining;
        public float CurrentSteerInput => steerInput;
        public float CurrentThrottleInput => throttleInput;
        public bool CurrentBrakeInput => brakeInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            trails = GetComponentsInChildren<TrailRenderer>();
            surface = GetComponent<SurfaceResponseProbe>();
            if (surface == null) surface = gameObject.AddComponent<SurfaceResponseProbe>();
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
            var accelerationMultiplier = surface == null ? 1f : surface.AccelerationMultiplier;
            var gripMultiplier = surface == null ? 1f : surface.GripMultiplier;
            var speedMultiplier = surface == null ? 1f : surface.SpeedMultiplier;

            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            var absoluteSpeedKph = Mathf.Abs(forwardSpeed) * 3.6f;
            var maxForwardSpeed = config.maxSpeedMetersPerSecond * speedMultiplier;
            var speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, maxForwardSpeed));

            UpdateDriftState(absoluteSpeedKph);
            UpdateNitroState();

            if (brakeInput && forwardSpeed > .25f)
            {
                localVelocity.z = Mathf.MoveTowards(localVelocity.z, 0f, 34f * Time.fixedDeltaTime);
                body.linearVelocity = transform.TransformDirection(localVelocity);
                forwardSpeed = localVelocity.z;
            }

            var accelerating = (throttleInput >= 0f ? config.acceleration : config.reverseAcceleration) * accelerationMultiplier;
            if (Mathf.Abs(forwardSpeed) < maxForwardSpeed || Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed))
                body.AddForce(transform.forward * (throttleInput * accelerating), ForceMode.Acceleration);

            if (nitroEngaged)
            {
                var nitroCurve = EvaluateCurve(config.nitroForceBySpeed, speed01, 1f);
                body.AddForce(transform.forward * (config.nitroForce * nitroCurve * accelerationMultiplier), ForceMode.Acceleration);
                NitroEnergy = Mathf.Max(0f, NitroEnergy - Time.fixedDeltaTime * config.nitroConsumptionPerSecond);
                if (NitroEnergy <= 0f) EndNitroBurst();
            }
            else
            {
                NitroEnergy = Mathf.Min(1f, NitroEnergy + Time.fixedDeltaTime * config.nitroRechargePerSecond);
            }

            UpdateDriftEnergy();

            var speedFactor = Mathf.Lerp(.42f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 12f));
            var direction = forwardSpeed < -0.5f ? -1f : 1f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, steerInput * config.steerStrengthDegrees * speedFactor * direction * Time.fixedDeltaTime, 0f));

            var curveMultiplier = IsDrifting
                ? EvaluateCurve(config.driftGripBySpeed, speed01, 1f)
                : EvaluateCurve(config.normalGripBySpeed, speed01, 1f);
            var activeGrip = (IsDrifting ? config.driftGrip : config.grip) * curveMultiplier * gripMultiplier;
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, activeGrip * Time.fixedDeltaTime);
            localVelocity.z = Mathf.Clamp(
                localVelocity.z,
                -maxForwardSpeed * 0.3f,
                maxForwardSpeed * (nitroEngaged ? 1.22f : 1f)
            );
            body.linearVelocity = transform.TransformDirection(localVelocity);

            foreach (var trail in trails) trail.emitting = IsDrifting || nitroEngaged;
        }

        private void UpdateNitroState()
        {
            nitroCooldownRemaining = Mathf.Max(0f, nitroCooldownRemaining - Time.fixedDeltaTime);

            if (nitroEngaged)
            {
                if (!nitroInput || Mathf.Abs(SpeedKph) <= 15f) EndNitroBurst();
                return;
            }

            if (!nitroInput || nitroCooldownRemaining > 0f) return;
            if (Mathf.Abs(SpeedKph) <= 15f || NitroEnergy < config.nitroMinimumActivationEnergy) return;
            nitroEngaged = true;
        }

        private void EndNitroBurst()
        {
            if (!nitroEngaged) return;
            nitroEngaged = false;
            nitroCooldownRemaining = Mathf.Max(nitroCooldownRemaining, config.nitroCooldownSeconds);
        }

        private void UpdateDriftState(float absoluteSpeedKph)
        {
            var qualified = driftInput
                && Mathf.Abs(steerInput) >= config.driftMinimumSteer
                && absoluteSpeedKph >= config.driftMinimumSpeedKph
                && (surface == null || surface.IsGrounded);

            IsDrifting = qualified;
            if (wasDrifting && !IsDrifting)
                driftReentryRemaining = config.driftReentryGuardSeconds;

            if (!IsDrifting)
                driftReentryRemaining = Mathf.Max(0f, driftReentryRemaining - Time.fixedDeltaTime);

            wasDrifting = IsDrifting;
        }

        private void UpdateDriftEnergy()
        {
            if (IsDrifting && driftReentryRemaining <= 0f)
                DriftEnergy = Mathf.Min(1f, DriftEnergy + config.driftChargePerSecond * Time.fixedDeltaTime);
            else
                DriftEnergy = Mathf.Max(0f, DriftEnergy - config.driftDecayPerSecond * Time.fixedDeltaTime);
        }

        private static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
        {
            return curve == null || curve.length == 0 ? fallback : Mathf.Max(0f, curve.Evaluate(time));
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
            var checkpoint = GetComponent<LastCheckpointTracker>();
            if (checkpoint != null && checkpoint.HasCheckpoint)
                transform.SetPositionAndRotation(checkpoint.Position + Vector3.up, checkpoint.Rotation);
            else
                transform.SetPositionAndRotation(spawnPosition + Vector3.up, spawnRotation);

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            nitroEngaged = false;
            nitroCooldownRemaining = 0f;
            IsDrifting = false;
            wasDrifting = false;
        }
    }
}
