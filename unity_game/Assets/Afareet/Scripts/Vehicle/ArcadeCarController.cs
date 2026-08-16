using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        private ArcadeCarConfig config;

        private Rigidbody body;
        private TrailRenderer[] trails;
        private ArcadeGroundSurfaceSensor surfaceSensor;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steerInput;
        private float throttleInput;
        private bool driftInput;
        private bool nitroInput;
        private bool brakeInput;
        private bool nitroWasActive;
        private float nitroCooldownRemaining;
        private float stuckDriveSeconds;
        private float recoveryInputLockRemaining;
        private ArcadeDriveModifier externalDriveModifier = ArcadeDriveModifier.Neutral();

        public bool AcceptsPlayerInput { get; set; }
        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
        public bool IsDrifting => DriftChargeActive;
        public bool IsGrounded => surfaceSensor != null && surfaceSensor.IsGrounded;
        public ArcadeSurfaceKind CurrentSurface => surfaceSensor == null ? ArcadeSurfaceKind.Asphalt : surfaceSensor.CurrentSurface;
        public bool NitroActive => config != null &&
            (surfaceSensor == null || surfaceSensor.IsGrounded) &&
            VehicleSpiritPolicy.CanActivateNitro(
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
        public float RecoveryInputLockRemaining => recoveryInputLockRemaining;
        public ArcadeDriveModifier ExternalDriveModifier => externalDriveModifier;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            trails = GetComponentsInChildren<TrailRenderer>();
            surfaceSensor = GetComponent<ArcadeGroundSurfaceSensor>();
            if (surfaceSensor == null)
                surfaceSensor = gameObject.AddComponent<ArcadeGroundSurfaceSensor>();
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
            var grounded = surfaceSensor.IsGrounded;
            if (AcceptsPlayerInput)
            {
                recoveryInputLockRemaining = Mathf.Max(0f, recoveryInputLockRemaining - Time.fixedDeltaTime);
                if (recoveryInputLockRemaining > 0f)
                    ClearDriveInputs();

                stuckDriveSeconds = VehicleRecoveryPolicy.AdvanceStuckTimer(
                    stuckDriveSeconds,
                    grounded,
                    SpeedKph,
                    throttleInput,
                    brakeInput,
                    Time.fixedDeltaTime);
                if (VehicleRecoveryPolicy.ShouldAutoRecover(stuckDriveSeconds))
                {
                    RecoverToTrack("auto-stuck");
                    return;
                }
            }
            else
            {
                // Never carry a partially accumulated stuck timer across countdown, pause,
                // results or restart boundaries. A later recovery must be earned by a fresh
                // continuous player drive-intent window.
                stuckDriveSeconds = 0f;
            }

            var surfaceResponse = config.SurfaceResponseFor(surfaceSensor.CurrentSurface);
            var driveModifier = externalDriveModifier;
            nitroCooldownRemaining = VehicleSpiritPolicy.AdvanceCooldown(nitroCooldownRemaining, Time.fixedDeltaTime);

            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            var lateralSlip = localVelocity.x;

            var driftRequestedOnGround = driftInput && grounded;
            DriftChargeActive = VehicleSpiritPolicy.CanChargeDrift(
                driftRequestedOnGround,
                steerInput,
                SpeedKph,
                lateralSlip,
                config.driftEnergyMinimumSteer,
                config.driftEnergyMinimumSpeedKph,
                config.driftEnergyMinimumSlipMetersPerSecond);
            DriftEnergy = VehicleSpiritPolicy.StepDriftEnergy(
                DriftEnergy,
                driftRequestedOnGround,
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

            if (grounded && brakeInput && forwardSpeed > .25f)
            {
                localVelocity.z = Mathf.MoveTowards(localVelocity.z, 0f, 34f * Time.fixedDeltaTime);
                body.linearVelocity = transform.TransformDirection(localVelocity);
                forwardSpeed = localVelocity.z;
            }

            var tractionDrive = VehicleHandlingPolicy.LimitDriveForTraction(
                throttleInput,
                lateralSlip,
                config.tractionSlipThresholdMetersPerSecond,
                config.tractionStrength);
            var accelerating = (tractionDrive >= 0f ? config.acceleration : config.reverseAcceleration) *
                               surfaceResponse.AccelerationMultiplier *
                               (float)driveModifier.AccelerationMultiplier;
            var surfaceMaxSpeed = config.maxSpeedMetersPerSecond *
                                  surfaceResponse.MaxSpeedMultiplier *
                                  (float)driveModifier.MaxSpeedMultiplier;
            if (grounded &&
                (Mathf.Abs(forwardSpeed) < surfaceMaxSpeed || Mathf.Sign(tractionDrive) != Mathf.Sign(forwardSpeed)))
            {
                body.AddForce(transform.forward * (tractionDrive * accelerating), ForceMode.Acceleration);
            }

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

            if (grounded)
            {
                var speedFactor = VehicleHandlingPolicy.SteeringSpeedFactor(forwardSpeed);
                var direction = forwardSpeed < -0.5f ? -1f : 1f;
                body.MoveRotation(body.rotation * Quaternion.Euler(
                    0f,
                    steerInput * config.steerStrengthDegrees * speedFactor * direction *
                    (float)driveModifier.SteeringAuthorityMultiplier * Time.fixedDeltaTime,
                    0f));

                var driftBlend = VehicleHandlingPolicy.DriftBlend(
                    driftInput,
                    steerInput,
                    lateralSlip,
                    config.driftGripMinimumSteer,
                    config.driftGripFullSlipMetersPerSecond);
                var effectiveGrip = VehicleHandlingPolicy.EffectiveGrip(
                                        config.grip,
                                        config.driftGrip,
                                        driftBlend,
                                        surfaceResponse.GripMultiplier) *
                                    (float)driveModifier.GripMultiplier;
                localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, effectiveGrip * Time.fixedDeltaTime);
            }

            var maxForwardSpeed = config.maxSpeedMetersPerSecond *
                                  (grounded ? surfaceResponse.MaxSpeedMultiplier : 1f) *
                                  (nitroActive ? 1.22f : 1f) *
                                  (float)driveModifier.MaxSpeedMultiplier;
            localVelocity.z = Mathf.Clamp(localVelocity.z, -config.maxSpeedMetersPerSecond * 0.3f, maxForwardSpeed);
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
            if (recoveryInputLockRemaining > 0f)
            {
                ClearDriveInputs();
                return;
            }

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
            ResetExternalDriveModifier();
            body.mass = config.massKilograms;
            body.linearDamping = config.linearDamping;
            body.angularDamping = config.angularDamping;
            body.centerOfMass = config.centerOfMass;
        }

        public void SetExternalDriveModifier(ArcadeDriveModifier modifier)
        {
            ArcadeDriveModifier.ValidateInitialized(modifier, nameof(modifier));
            externalDriveModifier = modifier;
        }

        public void ResetExternalDriveModifier()
        {
            externalDriveModifier = ArcadeDriveModifier.Neutral();
        }

        public void SetAiInput(float throttle, float steer, bool drift, bool nitro, bool brake = false)
        {
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            driftInput = drift;
            nitroInput = nitro;
            brakeInput = brake;
        }

        public void ResetToSpawn()
        {
            RecoverToTrack("manual");
        }

        private void RecoverToTrack(string reason)
        {
            var checkpoint = GetComponent<LastCheckpointTracker>();
            var source = "spawn";
            var targetPosition = spawnPosition + Vector3.up * VehicleRecoveryPolicy.RecoveryUpOffsetMeters;
            var targetRotation = spawnRotation;

            if (checkpoint != null && checkpoint.HasCheckpoint)
            {
                targetPosition = checkpoint.RecoveryPosition;
                targetRotation = checkpoint.Rotation;
                source = "checkpoint";
            }

            // Teleport the physics body directly; using Transform.SetPositionAndRotation on
            // an interpolated dynamic Rigidbody can leave the physics pose/contact state one
            // simulation step behind the visual Transform.
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = targetPosition;
            body.rotation = targetRotation;
            ClearDriveInputs();
            stuckDriveSeconds = 0f;
            recoveryInputLockRemaining = VehicleRecoveryPolicy.PostRecoveryInputLockSeconds;
            DriftEnergy = 0f;
            if (config != null && nitroWasActive)
                nitroCooldownRemaining = Mathf.Max(nitroCooldownRemaining, config.nitroCooldownSeconds);
            nitroWasActive = false;
            DriftChargeActive = false;

            Debug.Log(
                $"AFAREET_UVEH012_RECOVERY reason={reason} source={source} " +
                $"inputLock={VehicleRecoveryPolicy.PostRecoveryInputLockSeconds:0.00}s");
        }

        private void ClearDriveInputs()
        {
            throttleInput = 0f;
            steerInput = 0f;
            driftInput = false;
            nitroInput = false;
            brakeInput = false;
        }
    }
}
