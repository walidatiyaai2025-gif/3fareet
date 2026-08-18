using System;
using System.Collections.Generic;
using Afareet.Progression;

namespace Afareet.CareerRuntime
{
    public static class CareerPlayerProfileRewardApplication
    {
        public static CareerPlayerProfile ApplyWithSettledCoins(
            CareerPlayerProfile profile,
            CareerEventSettlement settlement,
            int settledCoinsGranted)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            ValidateSettledCoins(settlement.CoinsGranted, settledCoinsGranted);

            int nextCoins;
            int nextSpirit;
            checked
            {
                nextCoins = profile.Coins + settledCoinsGranted;
                nextSpirit = profile.Spirit + settlement.SpiritGranted;
            }

            var vehicles = new List<string>(profile.UnlockedVehicleIds);
            for (var index = 0; index < settlement.UnlockedVehicleIds.Count; index++)
                vehicles.Add(settlement.UnlockedVehicleIds[index]);

            return new CareerPlayerProfile(
                settlement.Progress,
                nextCoins,
                nextSpirit,
                vehicles);
        }

        public static void ValidateSettledCoins(int baseCoinsGranted, int settledCoinsGranted)
        {
            if (baseCoinsGranted < 0) throw new ArgumentOutOfRangeException(nameof(baseCoinsGranted));
            if (settledCoinsGranted < 0) throw new ArgumentOutOfRangeException(nameof(settledCoinsGranted));
            if (baseCoinsGranted == 0 && settledCoinsGranted != 0)
                throw new ArgumentException("A Career settlement with no coin reward cannot receive bonus coins.", nameof(settledCoinsGranted));
            if (settledCoinsGranted < baseCoinsGranted)
                throw new ArgumentOutOfRangeException(nameof(settledCoinsGranted), "Settled Career coins cannot be lower than the claimed base coin reward.");
        }
    }
}
