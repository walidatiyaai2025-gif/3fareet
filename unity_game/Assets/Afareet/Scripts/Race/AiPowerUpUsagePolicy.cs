using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public enum AiPowerUpDecisionReason
    {
        None = 0,
        DefensiveShield = 1,
        DefendFromChaser = 2,
        CatchUpOrFinalPush = 3,
        TargetLeader = 4,
        RewardOptimization = 5
    }

    public sealed class AiPowerUpAvailability
    {
        public PowerUpKind Kind { get; }
        public int Charges { get; }
        public double CooldownRemainingSeconds { get; }

        public bool IsUsable => Charges > 0 && CooldownRemainingSeconds <= 0d;

        public AiPowerUpAvailability(PowerUpKind kind, int charges, double cooldownRemainingSeconds)
        {
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (charges < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(charges));
            }

            if (!IsFinite(cooldownRemainingSeconds) || cooldownRemainingSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownRemainingSeconds));
            }

            Kind = kind;
            Charges = charges;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public sealed class AiPowerUpRaceSnapshot
    {
        public int Position { get; }
        public int FieldSize { get; }
        public double NormalizedProgress { get; }
        public double SpeedRatio { get; }
        public bool HasTargetAhead { get; }
        public double GapToTargetSeconds { get; }
        public bool HasChaserBehind { get; }
        public double GapFromChaserSeconds { get; }
        public bool IncomingHostilePressure { get; }
        public double RemainingRaceSeconds { get; }

        public AiPowerUpRaceSnapshot(
            int position,
            int fieldSize,
            double normalizedProgress,
            double speedRatio,
            bool hasTargetAhead,
            double gapToTargetSeconds,
            bool hasChaserBehind,
            double gapFromChaserSeconds,
            bool incomingHostilePressure,
            double remainingRaceSeconds)
        {
            if (fieldSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldSize));
            }

            if (position <= 0 || position > fieldSize)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            ValidateFiniteRange(normalizedProgress, 0d, 1d, nameof(normalizedProgress));
            ValidateFiniteRange(speedRatio, 0d, 2d, nameof(speedRatio));
            ValidateNonNegative(gapToTargetSeconds, nameof(gapToTargetSeconds));
            ValidateNonNegative(gapFromChaserSeconds, nameof(gapFromChaserSeconds));
            ValidateNonNegative(remainingRaceSeconds, nameof(remainingRaceSeconds));

            Position = position;
            FieldSize = fieldSize;
            NormalizedProgress = normalizedProgress;
            SpeedRatio = speedRatio;
            HasTargetAhead = hasTargetAhead;
            GapToTargetSeconds = gapToTargetSeconds;
            HasChaserBehind = hasChaserBehind;
            GapFromChaserSeconds = gapFromChaserSeconds;
            IncomingHostilePressure = incomingHostilePressure;
            RemainingRaceSeconds = remainingRaceSeconds;
        }

        private static void ValidateFiniteRange(double value, double min, double max, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        private static void ValidateNonNegative(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }
    }

    public sealed class AiPowerUpDecision
    {
        public PowerUpKind? Kind { get; }
        public AiPowerUpDecisionReason Reason { get; }
        public bool ShouldUse => Kind.HasValue;

        private AiPowerUpDecision(PowerUpKind? kind, AiPowerUpDecisionReason reason)
        {
            Kind = kind;
            Reason = reason;
        }

        public static AiPowerUpDecision Use(PowerUpKind kind, AiPowerUpDecisionReason reason)
        {
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (reason == AiPowerUpDecisionReason.None || !Enum.IsDefined(typeof(AiPowerUpDecisionReason), reason))
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }

            return new AiPowerUpDecision(kind, reason);
        }

        public static AiPowerUpDecision None()
        {
            return new AiPowerUpDecision(null, AiPowerUpDecisionReason.None);
        }
    }

    public static class AiPowerUpUsagePolicy
    {
        // Policy thresholds are deterministic defaults, not final game-balance approval.
        public const double DefensiveChaserGapSeconds = 1.25d;
        public const double TrafficCurseMaxTargetGapSeconds = 2.50d;
        public const double CatchUpMinimumGapSeconds = 0.80d;
        public const double CatchUpSpeedRatioThreshold = 0.98d;
        public const double FinalPushProgress = 0.82d;
        public const double FinalPushRemainingSeconds = 18d;
        public const double RewardOptimizationMinProgress = 0.65d;
        public const double RewardOptimizationMinLeadGapSeconds = 2.00d;

        private readonly struct InventoryIndex
        {
            public InventoryIndex(
                AiPowerUpAvailability asphaltShard,
                AiPowerUpAvailability nitroSpirit,
                AiPowerUpAvailability trafficCurse,
                AiPowerUpAvailability enchantedPound,
                AiPowerUpAvailability eyeShield)
            {
                AsphaltShard = asphaltShard;
                NitroSpirit = nitroSpirit;
                TrafficCurse = trafficCurse;
                EnchantedPound = enchantedPound;
                EyeShield = eyeShield;
            }

            public AiPowerUpAvailability AsphaltShard { get; }
            public AiPowerUpAvailability NitroSpirit { get; }
            public AiPowerUpAvailability TrafficCurse { get; }
            public AiPowerUpAvailability EnchantedPound { get; }
            public AiPowerUpAvailability EyeShield { get; }

            public AiPowerUpAvailability Get(PowerUpKind kind)
            {
                switch (kind)
                {
                    case PowerUpKind.AsphaltShard: return AsphaltShard;
                    case PowerUpKind.NitroSpirit: return NitroSpirit;
                    case PowerUpKind.TrafficCurse: return TrafficCurse;
                    case PowerUpKind.EnchantedPound: return EnchantedPound;
                    case PowerUpKind.EyeShield: return EyeShield;
                    default: throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }
        }

        private struct InventoryIndexBuilder
        {
            private AiPowerUpAvailability asphaltShard;
            private AiPowerUpAvailability nitroSpirit;
            private AiPowerUpAvailability trafficCurse;
            private AiPowerUpAvailability enchantedPound;
            private AiPowerUpAvailability eyeShield;

            public void Add(AiPowerUpAvailability item)
            {
                if (item == null)
                    throw new ArgumentException("AI power-up inventory cannot contain null entries.", "inventory");

                switch (item.Kind)
                {
                    case PowerUpKind.AsphaltShard:
                        EnsureMissing(asphaltShard, item.Kind);
                        asphaltShard = item;
                        break;
                    case PowerUpKind.NitroSpirit:
                        EnsureMissing(nitroSpirit, item.Kind);
                        nitroSpirit = item;
                        break;
                    case PowerUpKind.TrafficCurse:
                        EnsureMissing(trafficCurse, item.Kind);
                        trafficCurse = item;
                        break;
                    case PowerUpKind.EnchantedPound:
                        EnsureMissing(enchantedPound, item.Kind);
                        enchantedPound = item;
                        break;
                    case PowerUpKind.EyeShield:
                        EnsureMissing(eyeShield, item.Kind);
                        eyeShield = item;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(item));
                }
            }

            public InventoryIndex Build()
            {
                return new InventoryIndex(
                    asphaltShard,
                    nitroSpirit,
                    trafficCurse,
                    enchantedPound,
                    eyeShield);
            }

            private static void EnsureMissing(AiPowerUpAvailability existing, PowerUpKind kind)
            {
                if (existing != null)
                    throw new ArgumentException($"Duplicate AI power-up inventory entry for {kind}.", "inventory");
            }
        }

        public static AiPowerUpDecision Decide(
            AiPowerUpRaceSnapshot snapshot,
            IEnumerable<AiPowerUpAvailability> inventory)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            var byKind = BuildInventoryIndex(inventory);
            return DecideCore(
                snapshot,
                IsUsable(byKind, PowerUpKind.AsphaltShard),
                IsUsable(byKind, PowerUpKind.NitroSpirit),
                IsUsable(byKind, PowerUpKind.TrafficCurse),
                IsUsable(byKind, PowerUpKind.EnchantedPound),
                IsUsable(byKind, PowerUpKind.EyeShield));
        }

        internal static AiPowerUpDecision Decide(
            AiPowerUpRaceSnapshot snapshot,
            bool asphaltShardUsable,
            bool nitroSpiritUsable,
            bool trafficCurseUsable,
            bool enchantedPoundUsable,
            bool eyeShieldUsable)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return DecideCore(
                snapshot,
                asphaltShardUsable,
                nitroSpiritUsable,
                trafficCurseUsable,
                enchantedPoundUsable,
                eyeShieldUsable);
        }

        private static AiPowerUpDecision DecideCore(
            AiPowerUpRaceSnapshot snapshot,
            bool asphaltShardUsable,
            bool nitroSpiritUsable,
            bool trafficCurseUsable,
            bool enchantedPoundUsable,
            bool eyeShieldUsable)
        {
            if (snapshot.IncomingHostilePressure && eyeShieldUsable)
            {
                return AiPowerUpDecision.Use(PowerUpKind.EyeShield, AiPowerUpDecisionReason.DefensiveShield);
            }

            if (snapshot.HasChaserBehind &&
                snapshot.GapFromChaserSeconds <= DefensiveChaserGapSeconds &&
                asphaltShardUsable)
            {
                return AiPowerUpDecision.Use(PowerUpKind.AsphaltShard, AiPowerUpDecisionReason.DefendFromChaser);
            }

            if (snapshot.Position > 1 &&
                nitroSpiritUsable &&
                ShouldUseNitro(snapshot))
            {
                return AiPowerUpDecision.Use(PowerUpKind.NitroSpirit, AiPowerUpDecisionReason.CatchUpOrFinalPush);
            }

            if (snapshot.Position > 1 &&
                snapshot.HasTargetAhead &&
                snapshot.GapToTargetSeconds <= TrafficCurseMaxTargetGapSeconds &&
                trafficCurseUsable)
            {
                return AiPowerUpDecision.Use(PowerUpKind.TrafficCurse, AiPowerUpDecisionReason.TargetLeader);
            }

            if (snapshot.Position == 1 &&
                snapshot.NormalizedProgress >= RewardOptimizationMinProgress &&
                !snapshot.IncomingHostilePressure &&
                HasStableLead(snapshot) &&
                enchantedPoundUsable)
            {
                return AiPowerUpDecision.Use(PowerUpKind.EnchantedPound, AiPowerUpDecisionReason.RewardOptimization);
            }

            return AiPowerUpDecision.None();
        }

        private static InventoryIndex BuildInventoryIndex(IEnumerable<AiPowerUpAvailability> inventory)
        {
            var builder = new InventoryIndexBuilder();

            if (inventory is IReadOnlyList<AiPowerUpAvailability> indexed)
            {
                for (var index = 0; index < indexed.Count; index++)
                    builder.Add(indexed[index]);
            }
            else
            {
                foreach (var item in inventory)
                    builder.Add(item);
            }

            return builder.Build();
        }

        private static bool IsUsable(InventoryIndex inventory, PowerUpKind kind)
        {
            var item = inventory.Get(kind);
            return item != null && item.IsUsable;
        }

        private static bool ShouldUseNitro(AiPowerUpRaceSnapshot snapshot)
        {
            var meaningfulGap = snapshot.HasTargetAhead && snapshot.GapToTargetSeconds >= CatchUpMinimumGapSeconds;
            var belowPace = snapshot.SpeedRatio < CatchUpSpeedRatioThreshold;
            var finalProgressPush = snapshot.NormalizedProgress >= FinalPushProgress;
            var finalTimePush = snapshot.RemainingRaceSeconds <= FinalPushRemainingSeconds;
            return meaningfulGap || belowPace || finalProgressPush || finalTimePush;
        }

        private static bool HasStableLead(AiPowerUpRaceSnapshot snapshot)
        {
            return !snapshot.HasChaserBehind ||
                   snapshot.GapFromChaserSeconds >= RewardOptimizationMinLeadGapSeconds;
        }
    }
}
