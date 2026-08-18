using System;

namespace Afareet.CareerRuntime
{
    public interface ICareerGarageVehicleRuntime
    {
        string ActiveVehicleId { get; }
        void ValidateApply(string vehicleId);
        bool ApplyEquippedVehicle(string vehicleId);
    }

    public sealed class PassiveCareerGarageVehicleRuntime : ICareerGarageVehicleRuntime
    {
        public string ActiveVehicleId { get; private set; }

        public void ValidateApply(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage equipped vehicle id is required.", nameof(vehicleId));
        }

        public bool ApplyEquippedVehicle(string vehicleId)
        {
            ValidateApply(vehicleId);
            if (StringComparer.Ordinal.Equals(ActiveVehicleId, vehicleId))
                return false;
            ActiveVehicleId = vehicleId;
            return false;
        }
    }
}
