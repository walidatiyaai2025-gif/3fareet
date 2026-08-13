using System;

namespace Afareet.Support
{
    public static class CameraObstructionPolicy
    {
        public static float ResolveDistance(float desiredDistance, float hitDistance, float skin = 0.25f)
        {
            desiredDistance = Math.Max(0.5f, desiredDistance);
            if (hitDistance <= 0f || hitDistance >= desiredDistance) return desiredDistance;
            return Math.Max(0.5f, Math.Min(desiredDistance, hitDistance - Math.Max(0f, skin)));
        }

        public static bool IsObstructed(float desiredDistance, float hitDistance) => hitDistance > 0f && hitDistance < desiredDistance;
    }
}
