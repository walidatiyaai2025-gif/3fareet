using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public readonly struct AsphaltShardTrapPoint
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public AsphaltShardTrapPoint(double x, double y, double z)
        {
            X = ValidateFinite(x, nameof(x));
            Y = ValidateFinite(y, nameof(y));
            Z = ValidateFinite(z, nameof(z));
        }

        public double DistanceSquared(AsphaltShardTrapPoint other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        private static double ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    public sealed class AsphaltShardTrapDeployment
    {
        public long SequenceId { get; }
        public string SourceRacerId { get; }
        public AsphaltShardTrapPoint Position { get; }
        public double DeployedAtSeconds { get; }
        public double ArmedAtSeconds { get; }
        public double ExpiresAtSeconds { get; }
        public double TriggerRadiusMeters { get; }
        public bool IsConsumed { get; private set; }

        internal AsphaltShardTrapDeployment(
            long sequenceId,
            string sourceRacerId,
            AsphaltShardTrapPoint position,
            double deployedAtSeconds,
            double armedAtSeconds,
            double expiresAtSeconds,
            double triggerRadiusMeters)
        {
            if (sequenceId <= 0) throw new ArgumentOutOfRangeException(nameof(sequenceId));
            SequenceId = sequenceId;
            SourceRacerId = PowerUpRaceRuntime.ValidateRacerId(sourceRacerId, nameof(sourceRacerId));
            Position = position;
            DeployedAtSeconds = deployedAtSeconds;
            ArmedAtSeconds = armedAtSeconds;
            ExpiresAtSeconds = expiresAtSeconds;
            TriggerRadiusMeters = triggerRadiusMeters;
        }

        public bool IsArmed(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            return !IsConsumed && raceTimeSeconds >= ArmedAtSeconds && raceTimeSeconds < ExpiresAtSeconds;
        }

        public bool IsExpired(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            return raceTimeSeconds >= ExpiresAtSeconds;
        }

        internal void Consume()
        {
            if (IsConsumed) throw new InvalidOperationException("Asphalt Shard trap is already consumed.");
            IsConsumed = true;
        }

        private static void ValidateRaceTime(double raceTimeSeconds)
        {
            if (double.IsNaN(raceTimeSeconds) || double.IsInfinity(raceTimeSeconds) || raceTimeSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
        }
    }

    public sealed class AsphaltShardTrapRuntime
    {
        public const double ArmDelaySeconds = 0.35d;
        public const double LifetimeSeconds = 8d;
        public const double TriggerRadiusMeters = 2.25d;
        public const double PlacementBehindVehicleMeters = 2.75d;

        private readonly List<AsphaltShardTrapDeployment> deployments =
            new List<AsphaltShardTrapDeployment>();
        private long nextSequenceId = 1;

        public int ActiveCount => deployments.Count;

        public AsphaltShardTrapDeployment Deploy(
            string sourceRacerId,
            AsphaltShardTrapPoint position,
            double raceTimeSeconds)
        {
            PowerUpRaceRuntime.ValidateRacerId(sourceRacerId, nameof(sourceRacerId));
            ValidateRaceTime(raceTimeSeconds);

            var deployment = new AsphaltShardTrapDeployment(
                nextSequenceId++,
                sourceRacerId,
                position,
                raceTimeSeconds,
                raceTimeSeconds + ArmDelaySeconds,
                raceTimeSeconds + LifetimeSeconds,
                TriggerRadiusMeters);
            deployments.Add(deployment);
            return deployment;
        }

        public bool TryTrigger(
            string targetRacerId,
            AsphaltShardTrapPoint targetPosition,
            double raceTimeSeconds,
            out AsphaltShardTrapDeployment triggered)
        {
            PowerUpRaceRuntime.ValidateRacerId(targetRacerId, nameof(targetRacerId));
            ValidateRaceTime(raceTimeSeconds);
            RemoveInactive(raceTimeSeconds);

            var radiusSquared = TriggerRadiusMeters * TriggerRadiusMeters;
            for (var index = 0; index < deployments.Count; index++)
            {
                var deployment = deployments[index];
                if (!deployment.IsArmed(raceTimeSeconds)) continue;
                if (StringComparer.Ordinal.Equals(deployment.SourceRacerId, targetRacerId)) continue;
                if (deployment.Position.DistanceSquared(targetPosition) > radiusSquared) continue;

                deployment.Consume();
                triggered = deployment;
                return true;
            }

            triggered = null;
            return false;
        }

        public int Tick(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            return RemoveInactive(raceTimeSeconds);
        }

        public IReadOnlyList<AsphaltShardTrapDeployment> SnapshotActive(double raceTimeSeconds)
        {
            ValidateRaceTime(raceTimeSeconds);
            RemoveInactive(raceTimeSeconds);
            return deployments.AsReadOnly();
        }

        public void ResetRace()
        {
            deployments.Clear();
            nextSequenceId = 1;
        }

        private int RemoveInactive(double raceTimeSeconds)
        {
            var removed = 0;
            for (var index = deployments.Count - 1; index >= 0; index--)
            {
                var deployment = deployments[index];
                if (!deployment.IsConsumed && !deployment.IsExpired(raceTimeSeconds)) continue;
                deployments.RemoveAt(index);
                removed++;
            }
            return removed;
        }

        private static void ValidateRaceTime(double raceTimeSeconds)
        {
            if (double.IsNaN(raceTimeSeconds) || double.IsInfinity(raceTimeSeconds) || raceTimeSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(raceTimeSeconds));
        }
    }
}
