using System;

namespace Afareet.Vehicle
{
    public enum ArcadeSurfaceType
    {
        Asphalt,
        OffRoad,
        Boost,
        Slippery
    }

    public readonly struct ArcadeSurfaceMultipliers
    {
        public ArcadeSurfaceMultipliers(float grip, float acceleration, float maxSpeed)
        {
            Grip = grip;
            Acceleration = acceleration;
            MaxSpeed = maxSpeed;
        }

        public float Grip { get; }
        public float Acceleration { get; }
        public float MaxSpeed { get; }
    }

    public static class ArcadeSurfaceResponse
    {
        public static ArcadeSurfaceMultipliers For(ArcadeSurfaceType surface)
        {
            switch (surface)
            {
                case ArcadeSurfaceType.Asphalt: return new ArcadeSurfaceMultipliers(1f, 1f, 1f);
                case ArcadeSurfaceType.OffRoad: return new ArcadeSurfaceMultipliers(0.58f, 0.72f, 0.66f);
                case ArcadeSurfaceType.Boost: return new ArcadeSurfaceMultipliers(0.95f, 1.18f, 1.10f);
                case ArcadeSurfaceType.Slippery: return new ArcadeSurfaceMultipliers(0.42f, 0.75f, 0.82f);
                default: throw new ArgumentOutOfRangeException(nameof(surface), surface, null);
            }
        }
    }
}
