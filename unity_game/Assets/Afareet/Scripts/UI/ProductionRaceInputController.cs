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
        private bool hasMotionBaseline;

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

            motionBaseline = Input.acceleration;
            hasMotionBaseline = true;
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
                player.SetPlayerInput(0f, 0f, false, false, true);
                return;
            }

            var resolvedSteer = Mathf.Clamp(steer, -1f, 1f);
            var resolvedThrottle = Mathf.Clamp01(throttle);
            var resolvedDrift = drift;
            var resolvedNitro = nitro;
            var resolvedBrake = false;

            if (Application.isMobilePlatform && hasMotionBaseline)
            {
                var acceleration = Input.acceleration - motionBaseline;
                var landscapeLeft = Screen.orientation != ScreenOrientation.LandscapeRight;
                var steeringTilt = landscapeLeft ? -acceleration.y : acceleration.y;
                var forwardTilt = landscapeLeft ? -acceleration.x : acceleration.x;
                if (Mathf.Abs(resolvedSteer) < .01f)
                    resolvedSteer = MobileDriveInputPolicy.ResolveTiltSteer(steeringTilt);
                resolvedNitro |= forwardTilt > .32f;
                resolvedBrake |= forwardTilt < -.32f;
            }

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

        private void EnsureConfigured()
        {
            if (player == null || race == null)
                throw new InvalidOperationException("ProductionRaceInputController must be configured before use.");
        }
    }
}
