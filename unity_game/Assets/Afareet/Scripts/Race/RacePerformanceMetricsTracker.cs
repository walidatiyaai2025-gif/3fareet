using System;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Race
{
    public static class DriftScorePolicy
    {
        public static double ScoreDelta(
            bool isDrifting,
            double speedKph,
            double steerMagnitude,
            double deltaSeconds)
        {
            if (double.IsNaN(speedKph) || double.IsInfinity(speedKph))
                throw new ArgumentOutOfRangeException(nameof(speedKph));
            if (double.IsNaN(steerMagnitude) || double.IsInfinity(steerMagnitude))
                throw new ArgumentOutOfRangeException(nameof(steerMagnitude));
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!isDrifting || deltaSeconds <= 0d)
                return 0d;

            var speed = Math.Max(0d, Math.Abs(speedKph));
            var steer = Math.Max(0d, Math.Min(1d, Math.Abs(steerMagnitude)));
            return speed * (1d + steer) * deltaSeconds;
        }
    }

    public sealed class RacePerformanceMetricsTracker : MonoBehaviour
    {
        private ArcadeCarController player;
        private RaceDirector race;
        private double driftScore;
        private RaceRoundPhase previousPhase = RaceRoundPhase.Ready;

        public int DriftScore => (int)Math.Floor(Math.Max(0d, driftScore));

        public void Configure(ArcadeCarController playerCar, RaceDirector director)
        {
            player = playerCar != null ? playerCar : throw new ArgumentNullException(nameof(playerCar));
            race = director != null ? director : throw new ArgumentNullException(nameof(director));
            driftScore = 0d;
            previousPhase = race.Phase;
        }

        public void ResetMetrics()
        {
            driftScore = 0d;
        }

        private void FixedUpdate()
        {
            if (player == null || race == null)
                return;

            var phase = race.Phase;
            if (phase == RaceRoundPhase.Countdown && previousPhase != RaceRoundPhase.Countdown)
                ResetMetrics();
            previousPhase = phase;

            if (phase != RaceRoundPhase.Racing || race.IsPaused)
                return;

            driftScore += DriftScorePolicy.ScoreDelta(
                player.IsDrifting,
                player.SpeedKph,
                player.CurrentSteerInput,
                Time.fixedDeltaTime);
        }
    }
}
