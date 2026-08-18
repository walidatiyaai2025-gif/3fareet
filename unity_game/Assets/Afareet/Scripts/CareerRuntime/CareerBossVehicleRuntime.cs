using System;

namespace Afareet.CareerRuntime
{
    public interface ICareerBossVehicleRuntime
    {
        string ActiveBossVehicleId { get; }
        bool ApplyBossVehicle(string bossVehicleId);
        bool ClearBossVehicle();
    }

    public sealed class PassiveCareerBossVehicleRuntime : ICareerBossVehicleRuntime
    {
        public string ActiveBossVehicleId { get; private set; }

        public bool ApplyBossVehicle(string bossVehicleId)
        {
            if (string.IsNullOrWhiteSpace(bossVehicleId))
                throw new ArgumentException("Career boss vehicle id is required.", nameof(bossVehicleId));
            if (StringComparer.Ordinal.Equals(ActiveBossVehicleId, bossVehicleId))
                return false;
            ActiveBossVehicleId = bossVehicleId;
            return false;
        }

        public bool ClearBossVehicle()
        {
            if (ActiveBossVehicleId == null)
                return false;
            ActiveBossVehicleId = null;
            return false;
        }
    }
}
