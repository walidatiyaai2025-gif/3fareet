using System;
using System.Collections.Generic;
using Afareet.Race;

internal static class PowerUpVehicleEffectProjectionContractRunner
{
    private static int Main()
    {
        try
        {
            NeutralAndEyeShieldContract();
            IndividualProjectionContract();
            CompositionOrderContract();
            DuplicateKindContract();
            RuntimeAuthorityAndExpiryContract();
            Console.WriteLine("Power-up vehicle effect projection behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Power-up vehicle effect projection behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void NeutralAndEyeShieldContract()
    {
        var neutral = PowerUpVehicleEffectProjectionPolicy.Project(Array.Empty<ActivePowerUpEffect>());
        RequireNeutral(neutral, "empty projection");

        var shield = Active(PowerUpKind.EyeShield, magnitude: 1d);
        var shieldOnly = PowerUpVehicleEffectProjectionPolicy.Project(new[] { shield });
        Require(Math.Abs(shieldOnly.AccelerationMultiplier - 1d) < .0001d, "Eye Shield must not alter acceleration");
        Require(Math.Abs(shieldOnly.MaxSpeedMultiplier - 1d) < .0001d, "Eye Shield must not alter max speed");
        Require(Math.Abs(shieldOnly.SteeringAuthorityMultiplier - 1d) < .0001d, "Eye Shield must not alter steering");
        Require(Math.Abs(shieldOnly.GripMultiplier - 1d) < .0001d, "Eye Shield must not alter grip");
        Require(Math.Abs(shieldOnly.RewardMultiplier - 1d) < .0001d, "Eye Shield must not alter reward");
        Require(shieldOnly.SourceEffectCount == 1, "Eye Shield must still be represented in projection source count");
    }

    private static void IndividualProjectionContract()
    {
        var asphalt = PowerUpVehicleEffectProjectionPolicy.Project(new[]
        {
            Active(PowerUpKind.AsphaltShard, .35d)
        });
        Require(Math.Abs(asphalt.SteeringAuthorityMultiplier - .65d) < .0001d, "Asphalt Shard must reduce steering authority");
        Require(Math.Abs(asphalt.GripMultiplier - .65d) < .0001d, "Asphalt Shard must reduce grip");
        Require(Math.Abs(asphalt.AccelerationMultiplier - 1d) < .0001d, "Asphalt Shard must not alter acceleration projection");

        var nitro = PowerUpVehicleEffectProjectionPolicy.Project(new[]
        {
            Active(PowerUpKind.NitroSpirit, .20d)
        });
        Require(Math.Abs(nitro.AccelerationMultiplier - 1.2d) < .0001d, "Nitro Spirit must boost acceleration");
        Require(Math.Abs(nitro.MaxSpeedMultiplier - 1.2d) < .0001d, "Nitro Spirit must boost max speed");

        var traffic = PowerUpVehicleEffectProjectionPolicy.Project(new[]
        {
            Active(PowerUpKind.TrafficCurse, .25d)
        });
        Require(Math.Abs(traffic.AccelerationMultiplier - .75d) < .0001d, "Traffic Curse must slow acceleration");
        Require(Math.Abs(traffic.MaxSpeedMultiplier - .75d) < .0001d, "Traffic Curse must slow max speed");

        var reward = PowerUpVehicleEffectProjectionPolicy.Project(new[]
        {
            Active(PowerUpKind.EnchantedPound, 1.5d)
        });
        Require(Math.Abs(reward.RewardMultiplier - 1.5d) < .0001d, "Enchanted Pound must project reward multiplier");
    }

    private static void CompositionOrderContract()
    {
        var nitro = Active(PowerUpKind.NitroSpirit, .20d);
        var traffic = Active(PowerUpKind.TrafficCurse, .25d);
        var first = PowerUpVehicleEffectProjectionPolicy.Project(new[] { nitro, traffic });
        var second = PowerUpVehicleEffectProjectionPolicy.Project(new[] { traffic, nitro });

        Require(Math.Abs(first.AccelerationMultiplier - .9d) < .0001d, "Nitro + Traffic acceleration composition must be multiplicative");
        Require(Math.Abs(first.MaxSpeedMultiplier - .9d) < .0001d, "Nitro + Traffic max-speed composition must be multiplicative");
        Require(Math.Abs(first.AccelerationMultiplier - second.AccelerationMultiplier) < .0001d, "composition must be order-independent");
        Require(Math.Abs(first.MaxSpeedMultiplier - second.MaxSpeedMultiplier) < .0001d, "max-speed composition must be order-independent");
    }

    private static void DuplicateKindContract()
    {
        var threw = false;
        try
        {
            PowerUpVehicleEffectProjectionPolicy.Project(new[]
            {
                Active(PowerUpKind.NitroSpirit, .1d),
                Active(PowerUpKind.NitroSpirit, .2d)
            });
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        Require(threw, "duplicate active effect kinds must fail closed");
    }

    private static void RuntimeAuthorityAndExpiryContract()
    {
        var runtime = new PowerUpRaceRuntime(
            PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
            new[]
            {
                new PowerUpRacerRegistration("player"),
                new PowerUpRacerRegistration("rival")
            });

        var initial = runtime.GetVehicleEffectProjection("player", 0d);
        RequireNeutral(initial, "initial runtime projection");

        var nitro = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
        var curse = runtime.TryUse("rival", PowerUpKind.TrafficCurse, "player", .1d);
        Require(nitro.Status == PowerUpRuntimeUseStatus.Used, "runtime Nitro use must succeed");
        Require(curse.Status == PowerUpRuntimeUseStatus.Used, "runtime Traffic Curse use must succeed");

        var active = runtime.GetVehicleEffectProjection("player", .1d);
        Require(active.SourceEffectCount == 2, "runtime projection must read both authoritative active effects");
        Require(Math.Abs(active.AccelerationMultiplier - .9d) < .0001d, "runtime projection must compose authoritative Nitro + Traffic effects");

        var expired = runtime.GetVehicleEffectProjection("player", 4d);
        RequireNeutral(expired, "expired runtime projection");

        runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 6d);
        runtime.ResetRace();
        var reset = runtime.GetVehicleEffectProjection("player", 6d);
        RequireNeutral(reset, "reset runtime projection");
    }

    private static ActivePowerUpEffect Active(PowerUpKind kind, double magnitude)
    {
        return new ActivePowerUpEffect(
            new PowerUpEffectSpec(kind, 10d, magnitude, PowerUpRefreshPolicy.RefreshDuration),
            0d);
    }

    private static void RequireNeutral(PowerUpVehicleEffectProjection projection, string context)
    {
        Require(Math.Abs(projection.AccelerationMultiplier - 1d) < .0001d, $"{context} acceleration must be neutral");
        Require(Math.Abs(projection.MaxSpeedMultiplier - 1d) < .0001d, $"{context} max speed must be neutral");
        Require(Math.Abs(projection.SteeringAuthorityMultiplier - 1d) < .0001d, $"{context} steering must be neutral");
        Require(Math.Abs(projection.GripMultiplier - 1d) < .0001d, $"{context} grip must be neutral");
        Require(Math.Abs(projection.RewardMultiplier - 1d) < .0001d, $"{context} reward must be neutral");
        Require(!projection.HasDriveModifier, $"{context} must report no drive modifier");
        Require(!projection.HasRewardModifier, $"{context} must report no reward modifier");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
