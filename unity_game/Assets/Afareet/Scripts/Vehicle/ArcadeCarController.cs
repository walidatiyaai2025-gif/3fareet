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
        private bool nitroWasActive;
        private float nitroCooldownRemaining;

        public bool AcceptsPlayerInput { get; set; }
        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsDrifting => driftInput && Mathf.Abs(steerInput) > 0.15f && Mathf.Abs(SpeedKph) > 25f;
        public bool NitroActive => config != null && VehicleSpiritPolicy.CanActivateNitro(
            nitroInput,
            NitroEnergy,
            nitroCooldownRemaining,
            Mathf.Abs(SpeedKph),
            config.nitroMinimumActivationEnergy,
            config.nitroMinimumSpeedKph);
        public bool NitroReady => config != null && VehicleSpiritPolicy.CanActivateNitro(
            true,
            NitroEnergy,
            nitroCooldownRemaining,
            Mathf.Abs(SpeedKph),
            config.nitroMinimumActivationEnergy,
            config.nitroMinimumSpeedKph);
        public float NitroEnergy { get; private set; } = 1f;
        public float NitroCooldownRemaining => nitroCooldownRemaining;
        public float DriftEnergy { get; private set; }
        public bool DriftChargeActive { get; private set; }
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
            nitroCooldownRemaining = VehicleSpiritPolicy.AdvanceCooldown(nitroCooldownRemaining, Time.fixedDeltaTime);

            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            var lateralSlip = localVelocity.x;

            DriftChargeActive = VehicleSpiritPolicy.CanChargeDrift(
                driftInput,
                steerInput,
                SpeedKph,
                lateralSlip,
                config.driftEnergyMinimumSteer,
                config.driftEnergyMinimumSpeedKph,
                config.driftEnergyMinimumSlipMetersPerSecond);
            DriftEnergy = VehicleSpiritPolicy.StepDriftEnergy(
                DriftEnergy,
                driftInput,
                steerInput,
                SpeedKph,
                lateralSlip,
                Time.fixedDeltaTime,
                config.driftEnergyGainPerSecond,
                config.driftEnergyDecayPerSecond,
                config.driftEnergyMinimumSteer,
                config.driftEnergyMinimumSpeedKph,
                config.driftEnergyMinimumSlipMetersPerSecond,
                config.driftEnergyFullGainSlipMetersPerSecond);

            if (brakeInput && forwardSpeed > .25f)
            {
                localVelocity.z = Mathf.MoveTowards(localVelocity.z, 0f, 34f * Time.fixedDeltaTime);
                body.linearVelocity = transform.TransformDirection(localVelocity);
                forwardSpeed = localVelocity.z;
            }
            var accelerating = throttleInput >= 0f ? config.acceleration : config.reverseAcceleration;
            if (Mathf.Abs(forwardSpeed) < config.maxSpeedMetersPerSecond || Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed))
                body.AddForce(transform.forward * (throttleInput * accelerating), ForceMode.Acceleration);

            var nitroActive = NitroActive;
            if (nitroWasActive && !nitroActive)
                nitroCooldownRemaining = Mathf.Max(nitroCooldownRemaining, config.nitroCooldownSeconds);

            if (nitroActive)
            {
                var forceScale = VehicleSpiritPolicy.NitroForceScale(
                    forwardSpeed,
                    config.maxSpeedMetersPerSecond,
                    config.nitroLowSpeedForceScale,
                    config.nitroFullForceSpeedRatio);
                body.AddForce(transform.forward * (config.nitroForce * forceScale), ForceMode.Acceleration);
                NitroEnergy = VehicleSpiritPolicy.StepNitroEnergy(
                    NitroEnergy,
                    true,
                    false,
                    Time.fixedDeltaTime,
                    config.nitroConsumptionPerSecond,
                    config.nitroRechargePerSecond);

                if (NitroEnergy <= 0f)
                {
                    nitroActive = false;
                    nitroCooldownRemaining = Mathf.Max(nitroCooldownRemaining, config.nitroCooldownSeconds);
                }
            }
            else
            {
                NitroEnergy = VehicleSpiritPolicy.StepNitroEnergy(
                    NitroEnergy,
                    false,
                    nitroCooldownRemaining <= 0f,
                    Time.fixedDeltaTime,
                    config.nitroConsumptionPerSecond,
                    config.nitroRechargePerSecond);
            }
            nitroWasActive = nitroActive;

            var speedFactor = Mathf.Lerp(.42f, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 12f));
            var direction = forwardSpeed < -0.5f ? -1f : 1f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, steerInput * config.steerStrengthDegrees * speedFactor * direction * Time.fixedDeltaTime, 0f));

            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, (driftInput ? config.driftGrip : config.grip) * Time.fixedDeltaTime);
            localVelocity.z = Mathf.Clamp(localVelocity.z, -config.maxSpeedMetersPerSecond * 0.3f, config.maxSpeedMetersPerSecond * (nitroActive ? 1.22f : 1f));
            body.linearVelocity = transform.TransformDirection(localVelocity);

            foreach (var trail in trails) trail.emitting = IsDrifting || nitroActive;
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
            if (config != null && (nitroWasActive || nitroInput))
                nitroCooldownRemaining = Mathf.Max(nitroCooldownRemaining, config.nitroCooldownSeconds);
            nitroWasActive = false;
            DriftChargeActive = false;
        }
    }
}
