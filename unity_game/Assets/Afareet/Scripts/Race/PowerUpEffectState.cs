using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public enum PowerUpKind
    {
        AsphaltShard = 0,
        NitroSpirit = 1,
        TrafficCurse = 2,
        EnchantedPound = 3,
        EyeShield = 4
    }

    public enum PowerUpRefreshPolicy
    {
        RefreshDuration = 0,
        IgnoreWhileActive = 1,
        ReplaceIfStronger = 2
    }

    public enum PowerUpApplyResult
    {
        Applied = 0,
        Refreshed = 1,
        Replaced = 2,
        IgnoredWhileActive = 3,
        BlockedByEyeShield = 4
    }

    public sealed class PowerUpEffectSpec
    {
        public PowerUpKind Kind { get; }
        public double DurationSeconds { get; }
        public double Magnitude { get; }
        public PowerUpRefreshPolicy RefreshPolicy { get; }

        public PowerUpEffectSpec(
            PowerUpKind kind,
            double durationSeconds,
            double magnitude,
            PowerUpRefreshPolicy refreshPolicy)
        {
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!IsFinite(durationSeconds) || durationSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (!IsFinite(magnitude) || magnitude < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            }

            if (!Enum.IsDefined(typeof(PowerUpRefreshPolicy), refreshPolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(refreshPolicy));
            }

            Kind = kind;
            DurationSeconds = durationSeconds;
            Magnitude = magnitude;
            RefreshPolicy = refreshPolicy;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public sealed class ActivePowerUpEffect
    {
        public PowerUpEffectSpec Spec { get; }
        public double AppliedAtSeconds { get; }
        public double ExpiresAtSeconds { get; }

        public ActivePowerUpEffect(PowerUpEffectSpec spec, double appliedAtSeconds)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            ValidateRaceTime(appliedAtSeconds, nameof(appliedAtSeconds));
            AppliedAtSeconds = appliedAtSeconds;
            ExpiresAtSeconds = appliedAtSeconds + spec.DurationSeconds;
        }

        public bool IsActiveAt(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds, nameof(raceTimeSeconds));
            return raceTimeSeconds < ExpiresAtSeconds;
        }

        public double RemainingSecondsAt(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds, nameof(raceTimeSeconds));
            return Math.Max(0d, ExpiresAtSeconds - raceTimeSeconds);
        }

        private static void ValidateRaceTime(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }
    }

    public sealed class PowerUpEffectState
    {
        private static readonly PowerUpKind[] AllPowerUpKinds =
            (PowerUpKind[])Enum.GetValues(typeof(PowerUpKind));

        private readonly Dictionary<PowerUpKind, ActivePowerUpEffect> activeEffects =
            new Dictionary<PowerUpKind, ActivePowerUpEffect>();
        private readonly IPowerUpPresentationSink presentationSink;
        private long nextPresentationSequenceId;
        private double lastObservedRaceTimeSeconds;

        public PowerUpEffectState()
            : this(null)
        {
        }

        public PowerUpEffectState(IPowerUpPresentationSink presentationSink)
        {
            this.presentationSink = presentationSink ?? NullPowerUpPresentationSink.Instance;
        }

        public int ActiveCount => activeEffects.Count;

        public PowerUpApplyResult Apply(PowerUpEffectSpec spec, double raceTimeSeconds)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            ObserveRaceTime(raceTimeSeconds);
            RemoveExpired(raceTimeSeconds);

            if (PowerUpEffectPolicy.IsHostile(spec.Kind) && IsActive(PowerUpKind.EyeShield, raceTimeSeconds))
            {
                EmitPresentation(
                    PowerUpPresentationEventKind.Blocked,
                    spec.Kind,
                    raceTimeSeconds,
                    spec.Magnitude);
                return PowerUpApplyResult.BlockedByEyeShield;
            }

            if (!activeEffects.TryGetValue(spec.Kind, out var existing))
            {
                activeEffects.Add(spec.Kind, new ActivePowerUpEffect(spec, raceTimeSeconds));
                EmitPresentation(
                    PowerUpPresentationEventKind.Applied,
                    spec.Kind,
                    raceTimeSeconds,
                    spec.Magnitude);
                return PowerUpApplyResult.Applied;
            }

            switch (spec.RefreshPolicy)
            {
                case PowerUpRefreshPolicy.RefreshDuration:
                    activeEffects[spec.Kind] = new ActivePowerUpEffect(spec, raceTimeSeconds);
                    EmitPresentation(
                        PowerUpPresentationEventKind.Refreshed,
                        spec.Kind,
                        raceTimeSeconds,
                        spec.Magnitude);
                    return PowerUpApplyResult.Refreshed;

                case PowerUpRefreshPolicy.IgnoreWhileActive:
                    return PowerUpApplyResult.IgnoredWhileActive;

                case PowerUpRefreshPolicy.ReplaceIfStronger:
                    if (spec.Magnitude > existing.Spec.Magnitude)
                    {
                        activeEffects[spec.Kind] = new ActivePowerUpEffect(spec, raceTimeSeconds);
                        EmitPresentation(
                            PowerUpPresentationEventKind.Replaced,
                            spec.Kind,
                            raceTimeSeconds,
                            spec.Magnitude);
                        return PowerUpApplyResult.Replaced;
                    }

                    return PowerUpApplyResult.IgnoredWhileActive;

                default:
                    throw new InvalidOperationException($"Unsupported refresh policy: {spec.RefreshPolicy}");
            }
        }

        public bool IsActive(PowerUpKind kind, double raceTimeSeconds)
        {
            ObserveRaceTime(raceTimeSeconds);
            if (!Enum.IsDefined(typeof(PowerUpKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!activeEffects.TryGetValue(kind, out var effect))
            {
                return false;
            }

            if (effect.IsActiveAt(raceTimeSeconds))
            {
                return true;
            }

            activeEffects.Remove(kind);
            EmitPresentation(
                PowerUpPresentationEventKind.Expired,
                kind,
                raceTimeSeconds,
                effect.Spec.Magnitude);
            return false;
        }

        public ActivePowerUpEffect GetActive(PowerUpKind kind, double raceTimeSeconds)
        {
            return IsActive(kind, raceTimeSeconds) ? activeEffects[kind] : null;
        }

        public IReadOnlyList<ActivePowerUpEffect> Snapshot(double raceTimeSeconds)
        {
            ObserveRaceTime(raceTimeSeconds);
            RemoveExpired(raceTimeSeconds);

            var snapshot = new List<ActivePowerUpEffect>(activeEffects.Values);
            snapshot.Sort((left, right) => left.Spec.Kind.CompareTo(right.Spec.Kind));
            return snapshot.AsReadOnly();
        }

        public int Tick(double raceTimeSeconds)
        {
            ObserveRaceTime(raceTimeSeconds);
            return RemoveExpired(raceTimeSeconds);
        }

        public void ResetRace()
        {
            activeEffects.Clear();
            EmitPresentation(
                PowerUpPresentationEventKind.RaceReset,
                null,
                lastObservedRaceTimeSeconds,
                0d);
        }

        private int RemoveExpired(double raceTimeSeconds)
        {
            if (activeEffects.Count == 0)
            {
                return 0;
            }

            // The power-up kind set is fixed and tiny. Scan that cached enum order instead of
            // allocating a temporary expired-kind List on every physics tick while any effect
            // is active. Removing by key outside Dictionary enumeration is safe and preserves
            // the same deterministic ascending-kind presentation event ordering.
            var removed = 0;
            for (var index = 0; index < AllPowerUpKinds.Length; index++)
            {
                var kind = AllPowerUpKinds[index];
                if (!activeEffects.TryGetValue(kind, out var effect))
                    continue;
                if (effect.IsActiveAt(raceTimeSeconds))
                    continue;

                activeEffects.Remove(kind);
                EmitPresentation(
                    PowerUpPresentationEventKind.Expired,
                    kind,
                    raceTimeSeconds,
                    effect.Spec.Magnitude);
                removed++;
            }

            return removed;
        }

        private void EmitPresentation(
            PowerUpPresentationEventKind eventKind,
            PowerUpKind? kind,
            double raceTimeSeconds,
            double magnitude)
        {
            nextPresentationSequenceId++;
            presentationSink.Publish(
                new PowerUpPresentationEvent(
                    nextPresentationSequenceId,
                    eventKind,
                    kind,
                    raceTimeSeconds,
                    magnitude));
        }

        private void ObserveRaceTime(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            lastObservedRaceTimeSeconds = raceTimeSeconds;
        }

        private static void ValidateRaceTime(double raceTimeSeconds)
        {
            if (double.IsNaN(raceTimeSeconds) || double.IsInfinity(raceTimeSeconds) || raceTimeSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
            }
        }
    }

    public static class PowerUpEffectPolicy
    {
        public static bool IsHostile(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.AsphaltShard:
                case PowerUpKind.TrafficCurse:
                    return true;
                case PowerUpKind.NitroSpirit:
                case PowerUpKind.EnchantedPound:
                case PowerUpKind.EyeShield:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}