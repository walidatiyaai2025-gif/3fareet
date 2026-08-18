using System;
using System.Collections.Generic;
using System.Linq;
using Afareet.Race;

internal static class PowerUpRuntimeContractRunner
{
    private static int Main()
    {
        try
        {
            InventoryAndCooldownContract();
            EyeShieldConsumptionContract();
            IgnoredUseContract();
            AiExecutionContract();
            TargetGateContract();
            DeployableTrapFlow();
            TickAndResetContract();
            Console.WriteLine("Power-up runtime behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Power-up runtime behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void InventoryAndCooldownContract()
    {
        var runtime = Runtime(Rules(nitroCharges: 2, nitroCooldown: 5d));
        var first = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
        var cooldown = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 1d);
        var second = runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 5d);
        Require(first.Status == PowerUpRuntimeUseStatus.Used, "first Nitro use must succeed");
        Require(first.RemainingCharges == 1, "first Nitro use must consume one charge");
        Require(cooldown.Status == PowerUpRuntimeUseStatus.CooldownActive, "cooldown must gate early reuse");
        Require(cooldown.RemainingCharges == 1, "cooldown gate must not consume charge");
        Require(Math.Abs(cooldown.CooldownRemainingSeconds - 4d) < .0001d, "cooldown remainder must be deterministic");
        Require(second.Status == PowerUpRuntimeUseStatus.Used && second.RemainingCharges == 0, "Nitro must be reusable exactly at cooldown boundary");
    }

    private static void EyeShieldConsumptionContract()
    {
        var runtime = Runtime(Rules(eyeShieldCharges: 1, trafficCharges: 2));
        var shield = runtime.TryUse("player", PowerUpKind.EyeShield, null, 0d);
        var blocked = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "player", .1d);
        Require(shield.Status == PowerUpRuntimeUseStatus.Used, "Eye Shield must apply to self");
        Require(blocked.Status == PowerUpRuntimeUseStatus.BlockedByEyeShield, "hostile use must report Eye Shield block");
        Require(blocked.Consumed && blocked.RemainingCharges == 1, "blocked hostile use must still consume its real attempt");
        Require(runtime.GetActiveEffect("player", PowerUpKind.TrafficCurse, .1d) == null, "blocked hostile effect must not become active");
    }

    private static void IgnoredUseContract()
    {
        var runtime = Runtime(Rules(enchantedCharges: 2, enchantedCooldown: 4d));
        var first = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 0d);
        var ignored = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 4d);
        var inventory = runtime.GetInventorySnapshot("player", 4d).Single(value => value.Kind == PowerUpKind.EnchantedPound);
        Require(first.Status == PowerUpRuntimeUseStatus.Used, "first Enchanted Pound use must succeed");
        Require(ignored.Status == PowerUpRuntimeUseStatus.IgnoredByEffectPolicy, "active IgnoreWhileActive effect must reject duplicate");
        Require(!ignored.Consumed, "ignored effect attempt must not be consumed");
        Require(inventory.Charges == 1 && inventory.CooldownRemainingSeconds <= 0d, "ignored attempt must preserve inventory and cooldown readiness");
    }

    private static void AiExecutionContract()
    {
        var runtime = Runtime(Rules(eyeShieldCharges: 1));
        var snapshot = new AiPowerUpRaceSnapshot(2, 3, .4d, 1d, true, 1d, false, 0d, true, 30d);
        var before = runtime.GetAiAvailability("rival-a", 0d).Single(value => value.Kind == PowerUpKind.EyeShield);
        var execution = runtime.ExecuteAiDecision("rival-a", snapshot, "player", null, 0d);
        var after = runtime.GetAiAvailability("rival-a", 0d).Single(value => value.Kind == PowerUpKind.EyeShield);
        Require(before.IsUsable, "AI availability must expose usable inventory");
        Require(execution.Decision.Kind == PowerUpKind.EyeShield, "hostile pressure must deterministically choose Eye Shield");
        Require(execution.UseResult != null && execution.UseResult.Status == PowerUpRuntimeUseStatus.Used, "AI decision must execute through TryUse");
        Require(after.Charges == 0, "AI execution must mutate authoritative inventory");
        Require(runtime.GetActiveEffect("rival-a", PowerUpKind.EyeShield, 0d) != null, "AI-selected Eye Shield must become active");
    }

    private static void TargetGateContract()
    {
        var runtime = Runtime(Rules(trafficCharges: 1));
        var missing = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, null, 0d);
        var self = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "rival-a", 0d);
        var unknown = runtime.TryUse("rival-a", PowerUpKind.TrafficCurse, "ghost", 0d);
        var snapshot = new AiPowerUpRaceSnapshot(2, 3, .5d, 1.05d, true, 1d, false, 0d, false, 40d);
        var execution = runtime.ExecuteAiDecision("rival-a", snapshot, "player", null, 0d);
        Require(missing.Status == PowerUpRuntimeUseStatus.MissingTarget, "targeted power-up must require target");
        Require(self.Status == PowerUpRuntimeUseStatus.InvalidTarget, "targeted hostile power-up must reject self target");
        Require(unknown.Status == PowerUpRuntimeUseStatus.UnknownTarget, "targeted power-up must reject unknown target");
        Require(execution.Decision.Kind == PowerUpKind.TrafficCurse, "eligible AI must choose Traffic Curse when higher priorities are unavailable");
        Require(execution.UseResult.TargetRacerId == "player", "AI execution must use caller-supplied deterministic target");
        Require(runtime.GetActiveEffect("player", PowerUpKind.TrafficCurse, 0d) != null, "targeted AI effect must apply to supplied opponent");
    }

    private static void DeployableTrapFlow()
    {
        var runtime = Runtime(Rules(asphaltCharges: 1));
        var traps = new AsphaltShardTrapRuntime();
        var deployUse = runtime.TryUse("player", PowerUpKind.AsphaltShard, null, 0d);
        Require(deployUse.Status == PowerUpRuntimeUseStatus.Used, "Asphalt Shard deployment must consume the authoritative inventory use");
        Require(deployUse.RemainingCharges == 0, "Asphalt Shard deployment must consume one charge");
        Require(deployUse.TargetRacerId == null, "world-deployable Asphalt Shard must not require an opponent target at deploy time");

        var deployment = traps.Deploy("player", new AsphaltShardTrapPoint(0d, 0d, 0d), 0d);
        Require(traps.ActiveCount == 1, "trap runtime must retain one active deployment");
        Require(!traps.TryTrigger("rival-a", new AsphaltShardTrapPoint(0d, 0d, 0d), .1d, out _), "trap must respect arm delay");
        Require(!traps.TryTrigger("player", new AsphaltShardTrapPoint(0d, 0d, 0d), 1d, out _), "trap hit its source racer");
        Require(traps.TryTrigger("rival-a", new AsphaltShardTrapPoint(1d, 0d, 0d), 1d, out var triggered), "armed opponent must trigger nearby trap");
        Require(triggered.SequenceId == deployment.SequenceId, "triggered deployment identity must remain deterministic");

        var hit = runtime.TryApplyDeployedEffect("player", "rival-a", PowerUpKind.AsphaltShard, 1d);
        Require(hit.Status == PowerUpRuntimeUseStatus.Used, "deployed trap hit must apply Asphalt Shard effect without a second inventory charge");
        Require(runtime.GetActiveEffect("rival-a", PowerUpKind.AsphaltShard, 1d) != null, "trap hit must become an active Asphalt Shard effect");
        Require(traps.Tick(1.01d) == 1 && traps.ActiveCount == 0, "consumed trap must leave active runtime deterministically");
        Require(!traps.TryTrigger("rival-b", new AsphaltShardTrapPoint(1d, 0d, 0d), 1.1d, out _), "one-shot trap triggered twice");
    }

    private static void TickAndResetContract()
    {
        var runtime = Runtime(Rules(nitroCharges: 2, effectDuration: 1d));
        runtime.TryUse("rival-a", PowerUpKind.NitroSpirit, null, 0d);
        runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 0d);
        var tick = runtime.TickAll(2d);
        Require(tick.Select(value => value.RacerId).SequenceEqual(new[] { "player", "rival-a", "rival-b" }), "TickAll must be stable by ordinal racer ID");
        Require(tick.Single(value => value.RacerId == "player").ExpiredEffectCount == 1, "player expiration must be counted");
        Require(tick.Single(value => value.RacerId == "rival-a").ExpiredEffectCount == 1, "rival expiration must be counted");
        runtime.TryUse("player", PowerUpKind.NitroSpirit, null, 2d);
        runtime.ResetRace();
        var restored = runtime.GetInventorySnapshot("player", 2d).Single(value => value.Kind == PowerUpKind.NitroSpirit);
        Require(runtime.GetActiveEffect("player", PowerUpKind.NitroSpirit, 2d) == null, "reset must clear active effects");
        Require(restored.Charges == 2 && restored.CooldownRemainingSeconds <= 0d, "reset must restore initial charges and clear cooldowns");
    }

    private static PowerUpRaceRuntime Runtime(PowerUpRuntimeRuleset ruleset) => new PowerUpRaceRuntime(ruleset, new[]
    {
        new PowerUpRacerRegistration("rival-b"), new PowerUpRacerRegistration("player"), new PowerUpRacerRegistration("rival-a")
    });

    private static PowerUpRuntimeRuleset Rules(int nitroCharges = 0, double nitroCooldown = 0d, int eyeShieldCharges = 0,
        int trafficCharges = 0, int enchantedCharges = 0, double enchantedCooldown = 0d, int asphaltCharges = 0,
        double effectDuration = 10d) => new PowerUpRuntimeRuleset(new List<PowerUpRuntimeRule>
    {
        Rule(PowerUpKind.AsphaltShard, asphaltCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.WorldDeployable),
        Rule(PowerUpKind.NitroSpirit, nitroCharges, nitroCooldown, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Self),
        Rule(PowerUpKind.TrafficCurse, trafficCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Opponent),
        Rule(PowerUpKind.EnchantedPound, enchantedCharges, enchantedCooldown, effectDuration, PowerUpRefreshPolicy.IgnoreWhileActive, PowerUpRuntimeTargetMode.Self),
        Rule(PowerUpKind.EyeShield, eyeShieldCharges, 0d, effectDuration, PowerUpRefreshPolicy.RefreshDuration, PowerUpRuntimeTargetMode.Self)
    });

    private static PowerUpRuntimeRule Rule(PowerUpKind kind, int charges, double cooldown, double duration,
        PowerUpRefreshPolicy refreshPolicy, PowerUpRuntimeTargetMode targetMode) => new PowerUpRuntimeRule(
        kind, new PowerUpEffectSpec(kind, duration, 1d, refreshPolicy), charges, cooldown, targetMode);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
