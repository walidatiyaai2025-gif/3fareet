using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public enum PowerUpRuntimeTargetMode
    {
        Self = 0,
        Opponent = 1,
        WorldDeployable = 2
    }

    public enum PowerUpRuntimeUseStatus
    {
        Used = 0,
        BlockedByEyeShield = 1,
        IgnoredByEffectPolicy = 2,
        NoCharges = 3,
        CooldownActive = 4,
        MissingTarget = 5,
        InvalidTarget = 6,
        UnknownSource = 7,
        UnknownTarget = 8
    }

    public sealed class PowerUpRuntimeRule
    {
        public PowerUpKind Kind { get; }
        public PowerUpEffectSpec EffectSpec { get; }
        public int InitialCharges { get; }
        public double CooldownSeconds { get; }
        public PowerUpRuntimeTargetMode TargetMode { get; }

        public PowerUpRuntimeRule(
            PowerUpKind kind,
            PowerUpEffectSpec effectSpec,
            int initialCharges,
            double cooldownSeconds,
            PowerUpRuntimeTargetMode targetMode)
        {
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            EffectSpec = effectSpec ?? throw new ArgumentNullException(nameof(effectSpec));
            if (effectSpec.Kind != kind)
            {
                throw new ArgumentException("Runtime rule kind must match the effect spec kind.", nameof(effectSpec));
            }

            if (initialCharges < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCharges));
            }

            if (!IsFinite(cooldownSeconds) || cooldownSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            }

            if (!Enum.IsDefined(typeof(PowerUpRuntimeTargetMode), targetMode))
            {
                throw new ArgumentOutOfRangeException(nameof(targetMode));
            }

            Kind = kind;
            InitialCharges = initialCharges;
            CooldownSeconds = cooldownSeconds;
            TargetMode = targetMode;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public sealed class PowerUpRuntimeRuleset
    {
        private readonly Dictionary<PowerUpKind, PowerUpRuntimeRule> rules =
            new Dictionary<PowerUpKind, PowerUpRuntimeRule>();

        public PowerUpRuntimeRuleset(IEnumerable<PowerUpRuntimeRule> runtimeRules)
        {
            if (runtimeRules == null)
            {
                throw new ArgumentNullException(nameof(runtimeRules));
            }

            foreach (var rule in runtimeRules)
            {
                if (rule == null)
                {
                    throw new ArgumentException("Power-up runtime rules cannot contain null entries.", nameof(runtimeRules));
                }

                if (rules.ContainsKey(rule.Kind))
                {
                    throw new ArgumentException($"Duplicate runtime rule for {rule.Kind}.", nameof(runtimeRules));
                }

                var expectedTargetMode = ExpectedTargetMode(rule.Kind);
                if (rule.TargetMode != expectedTargetMode)
                {
                    throw new ArgumentException(
                        $"{rule.Kind} requires target mode {expectedTargetMode}.",
                        nameof(runtimeRules));
                }

                rules.Add(rule.Kind, rule);
            }

            foreach (PowerUpKind kind in Enum.GetValues(typeof(PowerUpKind)))
            {
                if (!rules.ContainsKey(kind))
                {
                    throw new ArgumentException($"Missing runtime rule for {kind}.", nameof(runtimeRules));
                }
            }
        }

        public PowerUpRuntimeRule Get(PowerUpKind kind)
        {
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return rules[kind];
        }

        public IReadOnlyList<PowerUpRuntimeRule> Snapshot()
        {
            var snapshot = new List<PowerUpRuntimeRule>();
            foreach (PowerUpKind kind in Enum.GetValues(typeof(PowerUpKind)))
            {
                snapshot.Add(rules[kind]);
            }

            return snapshot.AsReadOnly();
        }

        private static PowerUpRuntimeTargetMode ExpectedTargetMode(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.AsphaltShard:
                    return PowerUpRuntimeTargetMode.WorldDeployable;
                case PowerUpKind.TrafficCurse:
                    return PowerUpRuntimeTargetMode.Opponent;
                case PowerUpKind.NitroSpirit:
                case PowerUpKind.EnchantedPound:
                case PowerUpKind.EyeShield:
                    return PowerUpRuntimeTargetMode.Self;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    public sealed class PowerUpRacerRegistration
    {
        public string RacerId { get; }
        public IPowerUpPresentationSink PresentationSink { get; }

        public PowerUpRacerRegistration(string racerId, IPowerUpPresentationSink presentationSink = null)
        {
            RacerId = PowerUpRaceRuntime.ValidateRacerId(racerId, nameof(racerId));
            PresentationSink = presentationSink;
        }
    }

    public sealed class PowerUpInventorySnapshot
    {
        public PowerUpKind Kind { get; }
        public int Charges { get; }
        public double CooldownRemainingSeconds { get; }
        public bool IsUsable => Charges > 0 && CooldownRemainingSeconds <= 0d;

        public PowerUpInventorySnapshot(PowerUpKind kind, int charges, double cooldownRemainingSeconds)
        {
            Kind = kind;
            Charges = charges;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
        }
    }

    public sealed class PowerUpRuntimeUseResult
    {
        public PowerUpRuntimeUseStatus Status { get; }
        public PowerUpKind Kind { get; }
        public string SourceRacerId { get; }
        public string TargetRacerId { get; }
        public PowerUpApplyResult? EffectResult { get; }
        public int RemainingCharges { get; }
        public double CooldownRemainingSeconds { get; }

        public bool Consumed =>
            Status == PowerUpRuntimeUseStatus.Used ||
            Status == PowerUpRuntimeUseStatus.BlockedByEyeShield;

        public PowerUpRuntimeUseResult(
            PowerUpRuntimeUseStatus status,
            PowerUpKind kind,
            string sourceRacerId,
            string targetRacerId,
            PowerUpApplyResult? effectResult,
            int remainingCharges,
            double cooldownRemainingSeconds)
        {
            Status = status;
            Kind = kind;
            SourceRacerId = sourceRacerId;
            TargetRacerId = targetRacerId;
            EffectResult = effectResult;
            RemainingCharges = remainingCharges;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
        }
    }

    public sealed class PowerUpRuntimeTickResult
    {
        public string RacerId { get; }
        public int ExpiredEffectCount { get; }

        public PowerUpRuntimeTickResult(string racerId, int expiredEffectCount)
        {
            RacerId = racerId;
            ExpiredEffectCount = expiredEffectCount;
        }
    }

    public sealed class AiPowerUpExecutionResult
    {
        public AiPowerUpDecision Decision { get; }
        public PowerUpRuntimeUseResult UseResult { get; }

        public AiPowerUpExecutionResult(AiPowerUpDecision decision, PowerUpRuntimeUseResult useResult)
        {
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
            UseResult = useResult;
        }
    }

    public sealed class PowerUpRaceRuntime
    {
        private sealed class InventorySlot
        {
            public int InitialCharges { get; }
            public int Charges { get; set; }
            public double ReadyAtSeconds { get; set; }

            public InventorySlot(int initialCharges)
            {
                InitialCharges = initialCharges;
                Charges = initialCharges;
            }

            public void Reset()
            {
                Charges = InitialCharges;
                ReadyAtSeconds = 0d;
            }
        }

        private sealed class RacerState
        {
            public string RacerId { get; }
            public PowerUpEffectState Effects { get; }
            public Dictionary<PowerUpKind, InventorySlot> Inventory { get; } =
                new Dictionary<PowerUpKind, InventorySlot>();

            public RacerState(
                PowerUpRacerRegistration registration,
                IReadOnlyList<PowerUpRuntimeRule> rules)
            {
                RacerId = registration.RacerId;
                Effects = new PowerUpEffectState(registration.PresentationSink);
                foreach (var rule in rules)
                {
                    Inventory.Add(rule.Kind, new InventorySlot(rule.InitialCharges));
                }
            }
        }

        private readonly PowerUpRuntimeRuleset ruleset;
        private readonly SortedDictionary<string, RacerState> racers =
            new SortedDictionary<string, RacerState>(StringComparer.Ordinal);

        public PowerUpRaceRuntime(
            PowerUpRuntimeRuleset ruleset,
            IEnumerable<PowerUpRacerRegistration> registrations)
        {
            this.ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
            if (registrations == null)
            {
                throw new ArgumentNullException(nameof(registrations));
            }

            var rules = ruleset.Snapshot();
            foreach (var registration in registrations)
            {
                if (registration == null)
                {
                    throw new ArgumentException("Racer registrations cannot contain null entries.", nameof(registrations));
                }

                if (racers.ContainsKey(registration.RacerId))
                {
                    throw new ArgumentException(
                        $"Duplicate racer registration for {registration.RacerId}.",
                        nameof(registrations));
                }

                racers.Add(registration.RacerId, new RacerState(registration, rules));
            }

            if (racers.Count == 0)
            {
                throw new ArgumentException("At least one racer registration is required.", nameof(registrations));
            }
        }

        public IReadOnlyList<PowerUpInventorySnapshot> GetInventorySnapshot(
            string racerId,
            double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            var racer = GetRacerOrThrow(racerId);
            var snapshot = new List<PowerUpInventorySnapshot>();

            foreach (PowerUpKind kind in Enum.GetValues(typeof(PowerUpKind)))
            {
                var slot = racer.Inventory[kind];
                snapshot.Add(new PowerUpInventorySnapshot(
                    kind,
                    slot.Charges,
                    Math.Max(0d, slot.ReadyAtSeconds - raceTimeSeconds)));
            }

            return snapshot.AsReadOnly();
        }

        public IReadOnlyList<AiPowerUpAvailability> GetAiAvailability(
            string racerId,
            double raceTimeSeconds)
        {
            var inventory = GetInventorySnapshot(racerId, raceTimeSeconds);
            var availability = new List<AiPowerUpAvailability>(inventory.Count);
            foreach (var item in inventory)
            {
                availability.Add(new AiPowerUpAvailability(
                    item.Kind,
                    item.Charges,
                    item.CooldownRemainingSeconds));
            }

            return availability.AsReadOnly();
        }

        public PowerUpRuntimeUseResult TryUse(
            string sourceRacerId,
            PowerUpKind kind,
            string targetRacerId,
            double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!TryGetRacer(sourceRacerId, out var source))
            {
                return GateResult(PowerUpRuntimeUseStatus.UnknownSource, kind, sourceRacerId, targetRacerId);
            }

            var rule = ruleset.Get(kind);
            var slot = source.Inventory[kind];
            if (slot.Charges <= 0)
            {
                return GateResult(PowerUpRuntimeUseStatus.NoCharges, kind, source.RacerId, targetRacerId, slot);
            }

            var cooldownRemaining = Math.Max(0d, slot.ReadyAtSeconds - raceTimeSeconds);
            if (cooldownRemaining > 0d)
            {
                return new PowerUpRuntimeUseResult(
                    PowerUpRuntimeUseStatus.CooldownActive,
                    kind,
                    source.RacerId,
                    targetRacerId,
                    null,
                    slot.Charges,
                    cooldownRemaining);
            }

            if (rule.TargetMode == PowerUpRuntimeTargetMode.WorldDeployable)
            {
                if (!string.IsNullOrWhiteSpace(targetRacerId))
                {
                    return GateResult(
                        PowerUpRuntimeUseStatus.InvalidTarget,
                        kind,
                        source.RacerId,
                        targetRacerId,
                        slot);
                }

                slot.Charges--;
                slot.ReadyAtSeconds = raceTimeSeconds + rule.CooldownSeconds;
                return new PowerUpRuntimeUseResult(
                    PowerUpRuntimeUseStatus.Used,
                    kind,
                    source.RacerId,
                    null,
                    null,
                    slot.Charges,
                    rule.CooldownSeconds);
            }

            var targetResolution = ResolveTarget(source, rule, targetRacerId);
            if (targetResolution.Status.HasValue)
            {
                return GateResult(
                    targetResolution.Status.Value,
                    kind,
                    source.RacerId,
                    targetRacerId,
                    slot);
            }

            var target = targetResolution.Target;
            return ApplyResolvedEffect(source, target, rule, slot, raceTimeSeconds, consumeInventory: true);
        }

        public PowerUpRuntimeUseResult TryApplyDeployedEffect(
            string sourceRacerId,
            string targetRacerId,
            PowerUpKind kind,
            double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            if (kind != PowerUpKind.AsphaltShard)
                throw new ArgumentException("Only Asphalt Shard may be applied through the deployable-effect bridge.", nameof(kind));

            if (!TryGetRacer(sourceRacerId, out var source))
                return GateResult(PowerUpRuntimeUseStatus.UnknownSource, kind, sourceRacerId, targetRacerId);
            if (string.IsNullOrWhiteSpace(targetRacerId))
                return GateResult(PowerUpRuntimeUseStatus.MissingTarget, kind, source.RacerId, targetRacerId, source.Inventory[kind]);
            if (StringComparer.Ordinal.Equals(source.RacerId, targetRacerId))
                return GateResult(PowerUpRuntimeUseStatus.InvalidTarget, kind, source.RacerId, targetRacerId, source.Inventory[kind]);
            if (!TryGetRacer(targetRacerId, out var target))
                return GateResult(PowerUpRuntimeUseStatus.UnknownTarget, kind, source.RacerId, targetRacerId, source.Inventory[kind]);

            var rule = ruleset.Get(kind);
            if (rule.TargetMode != PowerUpRuntimeTargetMode.WorldDeployable)
                throw new InvalidOperationException("Asphalt Shard runtime rule must remain WorldDeployable.");

            return ApplyResolvedEffect(
                source,
                target,
                rule,
                source.Inventory[kind],
                raceTimeSeconds,
                consumeInventory: false);
        }

        public AiPowerUpExecutionResult ExecuteAiDecision(
            string sourceRacerId,
            AiPowerUpRaceSnapshot snapshot,
            string targetAheadRacerId,
            string chaserBehindRacerId,
            double raceTimeSeconds)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var availability = GetAiAvailability(sourceRacerId, raceTimeSeconds);
            var decision = AiPowerUpUsagePolicy.Decide(snapshot, availability);
            if (!decision.ShouldUse)
            {
                return new AiPowerUpExecutionResult(decision, null);
            }

            var kind = decision.Kind.Value;
            string targetRacerId = null;
            if (kind == PowerUpKind.TrafficCurse)
            {
                targetRacerId = targetAheadRacerId;
            }

            var useResult = TryUse(sourceRacerId, kind, targetRacerId, raceTimeSeconds);
            return new AiPowerUpExecutionResult(decision, useResult);
        }

        public IReadOnlyList<PowerUpRuntimeTickResult> TickAll(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            var results = new List<PowerUpRuntimeTickResult>(racers.Count);
            foreach (var pair in racers)
            {
                results.Add(new PowerUpRuntimeTickResult(
                    pair.Key,
                    pair.Value.Effects.Tick(raceTimeSeconds)));
            }

            return results.AsReadOnly();
        }

        public ActivePowerUpEffect GetActiveEffect(
            string racerId,
            PowerUpKind kind,
            double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            return GetRacerOrThrow(racerId).Effects.GetActive(kind, raceTimeSeconds);
        }

        public void ResetRace()
        {
            foreach (var pair in racers)
            {
                pair.Value.Effects.ResetRace();
                foreach (var slot in pair.Value.Inventory.Values)
                {
                    slot.Reset();
                }
            }
        }

        private PowerUpRuntimeUseResult ApplyResolvedEffect(
            RacerState source,
            RacerState target,
            PowerUpRuntimeRule rule,
            InventorySlot slot,
            double raceTimeSeconds,
            bool consumeInventory)
        {
            var applyResult = target.Effects.Apply(rule.EffectSpec, raceTimeSeconds);
            if (applyResult == PowerUpApplyResult.IgnoredWhileActive)
            {
                return new PowerUpRuntimeUseResult(
                    PowerUpRuntimeUseStatus.IgnoredByEffectPolicy,
                    rule.Kind,
                    source.RacerId,
                    target.RacerId,
                    applyResult,
                    slot.Charges,
                    Math.Max(0d, slot.ReadyAtSeconds - raceTimeSeconds));
            }

            if (consumeInventory)
            {
                slot.Charges--;
                slot.ReadyAtSeconds = raceTimeSeconds + rule.CooldownSeconds;
            }

            var status = applyResult == PowerUpApplyResult.BlockedByEyeShield
                ? PowerUpRuntimeUseStatus.BlockedByEyeShield
                : PowerUpRuntimeUseStatus.Used;

            return new PowerUpRuntimeUseResult(
                status,
                rule.Kind,
                source.RacerId,
                target.RacerId,
                applyResult,
                slot.Charges,
                consumeInventory
                    ? rule.CooldownSeconds
                    : Math.Max(0d, slot.ReadyAtSeconds - raceTimeSeconds));
        }

        private (RacerState Target, PowerUpRuntimeUseStatus? Status) ResolveTarget(
            RacerState source,
            PowerUpRuntimeRule rule,
            string targetRacerId)
        {
            if (rule.TargetMode == PowerUpRuntimeTargetMode.WorldDeployable)
                throw new InvalidOperationException("World-deployable power-ups must be resolved by their deployment runtime.");

            if (rule.TargetMode == PowerUpRuntimeTargetMode.Self)
            {
                if (targetRacerId != null && !StringComparer.Ordinal.Equals(targetRacerId, source.RacerId))
                {
                    return (null, PowerUpRuntimeUseStatus.InvalidTarget);
                }

                return (source, null);
            }

            if (string.IsNullOrWhiteSpace(targetRacerId))
            {
                return (null, PowerUpRuntimeUseStatus.MissingTarget);
            }

            if (StringComparer.Ordinal.Equals(targetRacerId, source.RacerId))
            {
                return (null, PowerUpRuntimeUseStatus.InvalidTarget);
            }

            if (!TryGetRacer(targetRacerId, out var target))
            {
                return (null, PowerUpRuntimeUseStatus.UnknownTarget);
            }

            return (target, null);
        }

        private static PowerUpRuntimeUseResult GateResult(
            PowerUpRuntimeUseStatus status,
            PowerUpKind kind,
            string sourceRacerId,
            string targetRacerId,
            InventorySlot slot = null)
        {
            return new PowerUpRuntimeUseResult(
                status,
                kind,
                sourceRacerId,
                targetRacerId,
                null,
                slot?.Charges ?? 0,
                0d);
        }

        private bool TryGetRacer(string racerId, out RacerState racer)
        {
            if (string.IsNullOrWhiteSpace(racerId))
            {
                racer = null;
                return false;
            }

            return racers.TryGetValue(racerId, out racer);
        }

        private RacerState GetRacerOrThrow(string racerId)
        {
            var validated = ValidateRacerId(racerId, nameof(racerId));
            if (!racers.TryGetValue(validated, out var racer))
            {
                throw new KeyNotFoundException($"Unknown power-up racer: {validated}.");
            }

            return racer;
        }

        internal static string ValidateRacerId(string racerId, string paramName)
        {
            if (string.IsNullOrWhiteSpace(racerId) || racerId.Length > 64)
            {
                throw new ArgumentException("Racer ID must contain 1-64 transport-safe characters.", paramName);
            }

            for (var i = 0; i < racerId.Length; i++)
            {
                var value = racerId[i];
                var safe =
                    (value >= 'a' && value <= 'z') ||
                    (value >= 'A' && value <= 'Z') ||
                    (value >= '0' && value <= '9') ||
                    value == '-' || value == '_' || value == '.';
                if (!safe)
                {
                    throw new ArgumentException("Racer ID contains unsupported characters.", paramName);
                }
            }

            return racerId;
        }

        private static void ValidateRaceTime(double raceTimeSeconds)
        {
            if (double.IsNaN(raceTimeSeconds) || double.IsInfinity(raceTimeSeconds) || raceTimeSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
            }
        }
    }
}
