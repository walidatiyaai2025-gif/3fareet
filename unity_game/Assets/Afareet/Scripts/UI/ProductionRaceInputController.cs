using System;
using Afareet.Race;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.UI
{
    public sealed class ProductionRaceInputController : MonoBehaviour
    {
        private ArcadeCarController player;
        private RaceDirector race;
        private Vector3 motionBaseline;
        private ScreenOrientation motionBaselineOrientation;
        private bool hasMotionBaseline;
        private float smoothedTiltSteer;
        private float smoothedTiltThrottle;

        public bool MotionDrivingAvailable =>
            Application.isMobilePlatform && SystemInfo.supportsAccelerometer;

        public bool MotionDrivingActive =>
            MotionDrivingAvailable && hasMotionBaseline;

        public void Configure(ArcadeCarController playerCar, RaceDirector director)
        {
            player = playerCar != null ? playerCar : throw new ArgumentNullException(nameof(playerCar));
            race = director != null ? director : throw new ArgumentNullException(nameof(director));
        }

        public bool StartRace()
        {
            EnsureConfigured();
            if (race.Phase != RaceRoundPhase.Ready)
                return false;

            CalibrateMotionInput();
            player.SetPlayerInput(0f, 0f, false, false, true);
            race.StartRace();
            return true;
        }

        public void RecoverPlayer()
        {
            EnsureConfigured();
            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
                return;

            player.SetPlayerInput(0f, 0f, false, false, false);
            player.ResetToSpawn();
        }

        public void ApplyDriveFrame(
            float steer,
            float throttle,
            bool drift,
            bool nitro,
            bool brakeReverse)
        {
            EnsureConfigured();
            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
            {
                smoothedTiltSteer = 0f;
                smoothedTiltThrottle = 0f;
                player.SetPlayerInput(0f, 0f, false, false, true);
                return;
            }

            var resolvedSteer = Mathf.Clamp(steer, -1f, 1f);
            var resolvedThrottle = Mathf.Clamp01(throttle);
            var resolvedDrift = drift;
            var resolvedNitro = nitro;
            var resolvedBrake = false;

            if (MotionDrivingActive)
            {
                RecalibrateIfLandscapeOrientationChanged();

                var acceleration = Input.acceleration - motionBaseline;
                var landscapeRight = Screen.orientation == ScreenOrientation.LandscapeRight;
                var steeringTilt = landscapeRight ? acceleration.y : -acceleration.y;
                var forwardTilt = landscapeRight ? acceleration.x : -acceleration.x;

                var tiltSteerTarget = MobileDriveInputPolicy.ResolveTiltSteer(steeringTilt);
                var tiltThrottleTarget = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(forwardTilt);
                smoothedTiltSteer = MobileDriveInputPolicy.SmoothTiltSteer(
                    smoothedTiltSteer,
                    tiltSteerTarget,
                    Time.unscaledDeltaTime);
                smoothedTiltThrottle = MobileDriveInputPolicy.SmoothTiltThrottle(
                    smoothedTiltThrottle,
                    tiltThrottleTarget,
                    Time.unscaledDeltaTime);

                // Explicit touch steering wins immediately. Motion steering is the hands-free
                // default, while calibrated forward/back pitch boosts or coasts auto-cruise.
                if (Mathf.Abs(resolvedSteer) < .01f)
                    resolvedSteer = smoothedTiltSteer;
                resolvedThrottle = Mathf.Max(resolvedThrottle, smoothedTiltThrottle);
            }

            // Brake/reverse remains an explicit control and overrides any motion accelerator
            // demand. Tilt never activates Spirit/Nitro, reverse, or braking.
            if (brakeReverse)
                MobileDriveInputPolicy.ResolveBrakeReverse(player.SpeedKph, out resolvedThrottle, out resolvedBrake);

            player.SetPlayerInput(
                resolvedThrottle,
                resolvedSteer,
                resolvedDrift,
                resolvedNitro,
                resolvedBrake);
        }

        private void Update()
        {
            if (player == null || race == null)
                return;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (race.Phase == RaceRoundPhase.Ready && Input.GetKeyDown(KeyCode.Return))
                StartRace();
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !MotionDrivingAvailable || race == null)
                return;
            if (race.Phase != RaceRoundPhase.Racing || race.IsPaused)
                return;

            // Phone orientation/grip can shift while the app is backgrounded. Re-center on
            // resume so a stale accelerometer baseline cannot produce a sudden steering snap.
            CalibrateMotionInput();
        }

        private void CalibrateMotionInput()
        {
            smoothedTiltSteer = 0f;
            smoothedTiltThrottle = 0f;

            if (!MotionDrivingAvailable)
            {
                hasMotionBaseline = false;
                return;
            }

            motionBaseline = Input.acceleration;
            motionBaselineOrientation = Screen.orientation;
            hasMotionBaseline = true;
        }

        private void RecalibrateIfLandscapeOrientationChanged()
        {
            var orientation = Screen.orientation;
            if (orientation == motionBaselineOrientation)
                return;
            if (orientation != ScreenOrientation.LandscapeLeft &&
                orientation != ScreenOrientation.LandscapeRight)
                return;

            CalibrateMotionInput();
        }

        private void EnsureConfigured()
        {
            if (player == null || race == null)
                throw new InvalidOperationException("ProductionRaceInputController must be configured before use.");
        }
    }
}
