using System;
using System.Linq;
using Afareet.CareerRuntime;
using Afareet.Progression;
using Afareet.Race;

internal static class RaceRewardSettlementContractRunner
{
    private static int Main()
    {
        try
        {
            LegacyParityContract();
            RuntimeSettlementContract();
            SnapshotSurvivesResetContract();
            CareerWalletApplicationContract();
            RoundingContract();
            InvalidAndOverflowContract();
            Console.WriteLine("Race reward settlement behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Race reward settlement behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void LegacyParityContract()
    {
        var rule = PowerUpRuntimeDefaults.CreatePrototypeRuleset()
            .Snapshot()
            .Single(value => value.Kind == PowerUpKind.EnchantedPound);
        Require(Math.Abs(rule.EffectSpec.Magnitude - 2d) < .000001d,
            "Enchanted Pound prototype must retain legacy x2 reward parity");
    }

    private static void RuntimeSettlementContract()
    {
        var runtime = Runtime();
        var use = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 0d);
        Require(use.Status == PowerUpRuntimeUseStatus.Used, "Enchanted Pound must activate");

        var active = runtime.SettleReward("player", 250, 1d);
        Require(active.SettledRewardUnits == 500, "active Enchanted Pound must double reward units");
        Require(active.BonusRewardUnits == 250, "x2 settlement bonus must equal base reward");

        var expired = runtime.SettleReward("player", 250, 8.1d);
        Require(expired.SettledRewardUnits == 250, "expired Enchanted Pound must return to neutral reward");
    }

    private static void SnapshotSurvivesResetContract()
    {
        var runtime = Runtime();
        runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 5d);
        var snapshot = runtime.CaptureRewardSettlementSnapshot("player", 6d);
        runtime.ResetRace();

        var settlement = snapshot.Settle(350);
        Require(Math.Abs(snapshot.RewardMultiplier - 2d) < .000001d,
            "finish snapshot must retain the captured multiplier after reset");
        Require(settlement.SettledRewardUnits == 700,
            "captured x2 finish snapshot must settle after runtime reset");
    }

    private static void CareerWalletApplicationContract()
    {
        var baseProfile = new CareerPlayerProfile(
            CareerProgress.Empty(),
            coins: 100,
            spirit: 7,
            unlockedVehicleIds: new[] { "afareet_king" });
        var firstClaim = Settlement(
            new CareerReward(coins: 250, spirit: 5, unlockVehicleId: "djinn_spirit"));
        var doubled = RaceRewardSettlementPolicy.Settle(firstClaim.CoinsGranted, 2d);
        var applied = CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
            baseProfile,
            firstClaim,
            doubled.SettledRewardUnits);

        Require(applied.Coins == 600, "Career wallet must receive the settled x2 coin amount exactly once");
        Require(applied.Spirit == 12, "race coin multiplier must not modify Career Spirit rewards");
        Require(applied.IsVehicleUnlocked("afareet_king"), "existing vehicle unlock must remain present");
        Require(applied.IsVehicleUnlocked("djinn_spirit"), "vehicle unlock payload must survive adjusted coin application");

        var neutralClaim = Settlement(new CareerReward(coins: 250));
        var neutral = RaceRewardSettlementPolicy.Settle(neutralClaim.CoinsGranted, 1d);
        var neutralApplied = CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
            CareerPlayerProfile.Empty(),
            neutralClaim,
            neutral.SettledRewardUnits);
        Require(neutralApplied.Coins == 250, "neutral multiplier must preserve exact legacy Career coin value");

        var replay = EmptySettlement();
        var replayApplied = CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(applied, replay, 0);
        Require(replayApplied.Coins == applied.Coins && replayApplied.Spirit == applied.Spirit,
            "reward-less replay must not mutate wallet balances");

        var spiritOnly = Settlement(new CareerReward(spirit: 4));
        var spiritApplied = CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(applied, spiritOnly, 0);
        Require(spiritApplied.Coins == applied.Coins && spiritApplied.Spirit == applied.Spirit + 4,
            "non-coin rewards must remain independent from race coin settlement");

        Expect<ArgumentException>(() => CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
            CareerPlayerProfile.Empty(),
            spiritOnly,
            1));
        Expect<ArgumentOutOfRangeException>(() => CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
            CareerPlayerProfile.Empty(),
            neutralClaim,
            249));
        Expect<OverflowException>(() => CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(
            new CareerPlayerProfile(CareerProgress.Empty(), int.MaxValue, 0, Array.Empty<string>()),
            Settlement(new CareerReward(coins: 1)),
            1));
    }

    private static CareerEventSettlement Settlement(params CareerReward[] rewards)
    {
        var ids = rewards.Select((_, index) => $"reward_{index:00}").ToArray();
        return new CareerEventSettlement(
            Evaluation(),
            CareerProgress.Empty(),
            nodeCompletedNow: true,
            starsEarned: 3,
            grantedRewards: rewards,
            grantedRewardIds: ids);
    }

    private static CareerEventSettlement EmptySettlement()
    {
        return new CareerEventSettlement(
            Evaluation(),
            CareerProgress.Empty(),
            nodeCompletedNow: false,
            starsEarned: 0,
            grantedRewards: Array.Empty<CareerReward>(),
            grantedRewardIds: Array.Empty<string>());
    }

    private static CareerObjectiveEvaluation Evaluation()
    {
        return new CareerObjectiveEvaluation(new[]
        {
            new CareerObjectiveEvaluationEntry("finish_contract", 1d, 1d)
        });
    }

    private static void RoundingContract()
    {
        var settlement = RaceRewardSettlementPolicy.Settle(3, 1.5d);
        Require(settlement.SettledRewardUnits == 5,
            "fractional settlement must use midpoint rounding away from zero");
    }

    private static void InvalidAndOverflowContract()
    {
        Expect<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(-1, 1d));
        Expect<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, double.NaN));
        Expect<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, .99d));
        Expect<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, 5.01d));
        Expect<OverflowException>(() => RaceRewardSettlementPolicy.Settle(int.MaxValue, 2d));
    }

    private static PowerUpRaceRuntime Runtime()
    {
        return new PowerUpRaceRuntime(
            PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
            new[] { new PowerUpRacerRegistration("player") });
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}