using System;

namespace Afareet.Vehicle
{
    // Virtual racing-game impact response helper only.
    public static class ArcadeImpactResponse
    {
        public static float ClampVirtualImpulse(float requestedImpulse, float virtualMass, float maxVirtualDeltaSpeed)
        {
            if (virtualMass <= 0f) throw new ArgumentOutOfRangeException(nameof(virtualMass));
            if (maxVirtualDeltaSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(maxVirtualDeltaSpeed));

            var maximum = virtualMass * maxVirtualDeltaSpeed;
            if (requestedImpulse < -maximum) return -maximum;
            if (requestedImpulse > maximum) return maximum;
            return requestedImpulse;
        }
    }
}
