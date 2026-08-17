using System;
using System.Collections.Generic;
using Afareet.GarageRuntime;
using Afareet.Progression;

namespace Afareet.CareerRuntime
{
    public static class CareerGarageBridge
    {
        public static IReadOnlyList<string> ResolveUnlockedVehicleIds(
            CareerPlayerProfile profile,
            GarageCatalog catalog)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var seen = new HashSet<string>(StringComparer.Ordinal)
            {
                GarageCatalog.StarterVehicleId
            };
            var result = new List<string> { GarageCatalog.StarterVehicleId };

            for (var index = 0; index < profile.UnlockedVehicleIds.Count; index++)
            {
                var id = profile.UnlockedVehicleIds[index];
                if (!catalog.TryGet(id, out _))
                    throw new InvalidOperationException(
                        $"Career profile unlock '{id}' has no Garage catalog definition.");
                if (seen.Add(id)) result.Add(id);
            }

            return result.AsReadOnly();
        }

        public static GarageService CreateGarageService(
            CareerPlayerProfile profile,
            GarageCatalog catalog = null,
            GarageState initialState = null)
        {
            catalog ??= GarageCatalog.CreateDefault();
            var unlocked = ResolveUnlockedVehicleIds(profile ?? throw new ArgumentNullException(nameof(profile)), catalog);
            return new GarageService(catalog, unlocked, initialState);
        }

        public static void ValidateCareerVehicleRewardsOrThrow(
            IEnumerable<CareerNodeDefinition> definitions,
            GarageCatalog catalog = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            catalog ??= GarageCatalog.CreateDefault();

            foreach (var definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Career definitions cannot contain null entries.", nameof(definitions));

                for (var rewardIndex = 0; rewardIndex < definition.Rewards.Count; rewardIndex++)
                {
                    var reward = definition.Rewards[rewardIndex];
                    if (!reward.HasVehicleUnlock) continue;
                    if (!catalog.TryGet(reward.UnlockVehicleId, out _))
                        throw new InvalidOperationException(
                            $"Career reward '{definition.Node.Id}' unlocks unknown Garage vehicle '{reward.UnlockVehicleId}'.");
                }
            }
        }
    }
}
