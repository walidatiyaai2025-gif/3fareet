using System;
using Afareet.CareerRuntime;
using Afareet.GarageRuntime;
using Afareet.Race;
using Afareet.Vehicle;

namespace Afareet.Core
{
    public sealed class CareerGarageVehicleRuntimeController : ICareerGarageVehicleRuntime
    {
        private readonly GarageCatalog catalog;
        private readonly RaceDirector race;
        private readonly ArcadeCarController player;

        public string ActiveVehicleId { get; private set; }

        public CareerGarageVehicleRuntimeController(
            GarageCatalog garageCatalog,
            RaceDirector raceDirector,
            ArcadeCarController playerCar)
        {
            catalog = garageCatalog ?? throw new ArgumentNullException(nameof(garageCatalog));
            race = raceDirector != null ? raceDirector : throw new ArgumentNullException(nameof(raceDirector));
            player = playerCar != null ? playerCar : throw new ArgumentNullException(nameof(playerCar));
        }

        public void ValidateApply(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                throw new ArgumentException("Garage equipped vehicle id is required.", nameof(vehicleId));
            catalog.GetRequired(vehicleId);
            if (race.Phase == RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing)
                throw new InvalidOperationException("Garage vehicle equip cannot change during countdown or active racing.");
        }

        public bool ApplyEquippedVehicle(string vehicleId)
        {
            ValidateApply(vehicleId);
            var normalized = catalog.NormalizeStats(vehicleId);
            var profile = GarageVehiclePerformanceProjection.Project(normalized);
            player.SetVehiclePerformanceProfile(profile);

            if (StringComparer.Ordinal.Equals(ActiveVehicleId, vehicleId))
                return false;

            ActiveVehicleId = vehicleId;
            return true;
        }
    }
}
