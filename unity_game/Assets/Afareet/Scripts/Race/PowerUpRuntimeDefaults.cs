using System.Collections.Generic;

namespace Afareet.Race
{
    public static class PowerUpRuntimeDefaults
    {
        // Prototype tuning only. Values are deterministic bootstrap defaults, not final balance approval.
        // Enchanted Pound's x2 reward multiplier is retained behavioral parity from legacy PR #9.
        public static PowerUpRuntimeRuleset CreatePrototypeRuleset()
        {
            return new PowerUpRuntimeRuleset(new List<PowerUpRuntimeRule>
            {
                Rule(
                    PowerUpKind.AsphaltShard,
                    initialCharges: 1,
                    cooldownSeconds: 8d,
                    durationSeconds: 3d,
                    magnitude: .35d,
                    refreshPolicy: PowerUpRefreshPolicy.RefreshDuration,
                    targetMode: PowerUpRuntimeTargetMode.Opponent),
                Rule(
                    PowerUpKind.NitroSpirit,
                    initialCharges: 2,
                    cooldownSeconds: 6d,
                    durationSeconds: 2.5d,
                    magnitude: .20d,
                    refreshPolicy: PowerUpRefreshPolicy.RefreshDuration,
                    targetMode: PowerUpRuntimeTargetMode.Self),
                Rule(
                    PowerUpKind.TrafficCurse,
                    initialCharges: 1,
                    cooldownSeconds: 8d,
                    durationSeconds: 3.5d,
                    magnitude: .25d,
                    refreshPolicy: PowerUpRefreshPolicy.RefreshDuration,
                    targetMode: PowerUpRuntimeTargetMode.Opponent),
                Rule(
                    PowerUpKind.EnchantedPound,
                    initialCharges: 1,
                    cooldownSeconds: 12d,
                    durationSeconds: 8d,
                    magnitude: 2d,
                    refreshPolicy: PowerUpRefreshPolicy.IgnoreWhileActive,
                    targetMode: PowerUpRuntimeTargetMode.Self),
                Rule(
                    PowerUpKind.EyeShield,
                    initialCharges: 1,
                    cooldownSeconds: 10d,
                    durationSeconds: 4d,
                    magnitude: 1d,
                    refreshPolicy: PowerUpRefreshPolicy.RefreshDuration,
                    targetMode: PowerUpRuntimeTargetMode.Self)
            });
        }

        private static PowerUpRuntimeRule Rule(
            PowerUpKind kind,
            int initialCharges,
            double cooldownSeconds,
            double durationSeconds,
            double magnitude,
            PowerUpRefreshPolicy refreshPolicy,
            PowerUpRuntimeTargetMode targetMode)
        {
            return new PowerUpRuntimeRule(
                kind,
                new PowerUpEffectSpec(kind, durationSeconds, magnitude, refreshPolicy),
                initialCharges,
                cooldownSeconds,
                targetMode);
        }
    }
}
