using System;
using System.Collections.Generic;

namespace Afareet.Race
{
    public static class PowerUpRaceRuntimeProjectionExtensions
    {
        public static PowerUpVehicleEffectProjection GetVehicleEffectProjection(
            this PowerUpRaceRuntime runtime,
            string racerId,
            double raceTimeSeconds)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            var activeEffects = new List<ActivePowerUpEffect>();
            foreach (PowerUpKind kind in Enum.GetValues(typeof(PowerUpKind)))
            {
                var effect = runtime.GetActiveEffect(racerId, kind, raceTimeSeconds);
                if (effect != null)
                {
                    activeEffects.Add(effect);
                }
            }

            return PowerUpVehicleEffectProjectionPolicy.Project(activeEffects);
        }
    }
}
