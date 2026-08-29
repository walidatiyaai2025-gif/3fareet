using System;

namespace Afareet.Race
{
    public sealed class RaceRewardSettlement
    {
        public int BaseRewardUnits { get; }
        public double RewardMultiplier { get; }
        public int SettledRewardUnits { get; }
        public int BonusRewardUnits => SettledRewardUnits - BaseRewardUnits;
        public bool WasModified => SettledRewardUnits != BaseRewardUnits;

        public RaceRewardSettlement(
            int baseRewardUnits,
            double rewardMultiplier,
            int settledRewardUnits)
        {
            if (baseRewardUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(baseRewardUnits));
            RaceRewardSettlementPolicy.ValidateMultiplier(rewardMultiplier, nameof(rewardMultiplier));
            if (settledRewardUnits < baseRewardUnits)
                throw new ArgumentOutOfRangeException(nameof(settledRewardUnits));

            BaseRewardUnits = baseRewardUnits;
            RewardMultiplier = rewardMultiplier;
            SettledRewardUnits = settledRewardUnits;
        }
    }

    public sealed class RaceRewardSettlementSnapshot
    {
        public double RaceTimeSeconds { get; }
        public double RewardMultiplier { get; }

        public RaceRewardSettlementSnapshot(double raceTimeSeconds, double rewardMultiplier)
        {
            if (double.IsNaN(raceTimeSeconds) || double.IsInfinity(raceTimeSeconds) || raceTimeSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
            RaceRewardSettlementPolicy.ValidateMultiplier(rewardMultiplier, nameof(rewardMultiplier));

            RaceTimeSeconds = raceTimeSeconds;
            RewardMultiplier = rewardMultiplier;
        }

        public RaceRewardSettlement Settle(int baseRewardUnits)
        {
            return RaceRewardSettlementPolicy.Settle(baseRewardUnits, RewardMultiplier);
        }
    }

    public static class RaceRewardSettlementPolicy
    {
        public const double MinimumRewardMultiplier = 1d;
        public const double MaximumRewardMultiplier = 5d;

        public static RaceRewardSettlement Settle(int baseRewardUnits, double rewardMultiplier)
        {
            if (baseRewardUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(baseRewardUnits));
            ValidateMultiplier(rewardMultiplier, nameof(rewardMultiplier));

            var scaled = checked((decimal)baseRewardUnits * (decimal)rewardMultiplier);
            var rounded = decimal.Round(scaled, 0, MidpointRounding.AwayFromZero);
            if (rounded > int.MaxValue)
                throw new OverflowException("Settled race reward exceeds Int32 range.");

            return new RaceRewardSettlement(
                baseRewardUnits,
                rewardMultiplier,
                decimal.ToInt32(rounded));
        }

        internal static void ValidateMultiplier(double rewardMultiplier, string paramName)
        {
            if (double.IsNaN(rewardMultiplier) ||
                double.IsInfinity(rewardMultiplier) ||
                rewardMultiplier < MinimumRewardMultiplier ||
                rewardMultiplier > MaximumRewardMultiplier)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    $"Reward multiplier must be finite and within [{MinimumRewardMultiplier}, {MaximumRewardMultiplier}].");
            }
        }
    }

    public static class PowerUpRaceRewardSettlementExtensions
    {
        public static RaceRewardSettlementSnapshot CaptureRewardSettlementSnapshot(
            this PowerUpRaceRuntime runtime,
            string racerId,
            double raceTimeSeconds)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            var projection = runtime.GetVehicleEffectProjection(racerId, raceTimeSeconds);
            return new RaceRewardSettlementSnapshot(
                raceTimeSeconds,
                projection.RewardMultiplier);
        }

        public static RaceRewardSettlement SettleReward(
            this PowerUpRaceRuntime runtime,
            string racerId,
            int baseRewardUnits,
            double raceTimeSeconds)
        {
            return runtime
                .CaptureRewardSettlementSnapshot(racerId, raceTimeSeconds)
                .Settle(baseRewardUnits);
        }
    }
}
