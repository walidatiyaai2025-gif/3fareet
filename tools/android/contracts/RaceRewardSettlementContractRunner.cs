using System;
using System.Linq;
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
