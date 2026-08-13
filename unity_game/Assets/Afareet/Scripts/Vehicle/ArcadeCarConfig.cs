using UnityEngine;

namespace Afareet.Vehicle
{
    [CreateAssetMenu(menuName = "Afareet/Vehicle/Arcade Car Config", fileName = "ArcadeCarConfig")]
    public sealed class ArcadeCarConfig : ScriptableObject
    {
        [Header("Acceleration")]
        [Min(0.1f)] public float acceleration = 34f;
        [Min(0.1f)] public float reverseAcceleration = 15f;
        [Min(1f)] public float maxSpeedMetersPerSecond = 48f;
        [Min(0f)] public float nitroForce = 25f;

        [Header("Handling")]
        [Min(1f)] public float steerStrengthDegrees = 105f;
        [Min(0.1f)] public float grip = 8f;
        [Min(0.1f)] public float driftGrip = 2.1f;

        [Header("Body")]
        [Min(1f)] public float massKilograms = 1150f;
        [Min(0f)] public float linearDamping = 0.15f;
        [Min(0f)] public float angularDamping = 3.5f;
        public Vector3 centerOfMass = new(0f, -0.45f, 0.15f);

        [Header("Spirit")]
        [Range(0.01f, 1f)] public float nitroConsumptionPerSecond = 0.22f;
        [Range(0.01f, 1f)] public float nitroRechargePerSecond = 0.06f;

        public bool IsValid(out string error)
        {
            if (acceleration <= 0f || reverseAcceleration <= 0f)
                return Fail("Acceleration values must be positive.", out error);
            if (maxSpeedMetersPerSecond <= 0f || steerStrengthDegrees <= 0f)
                return Fail("Speed and steering values must be positive.", out error);
            if (grip <= 0f || driftGrip <= 0f || driftGrip >= grip)
                return Fail("Drift grip must be positive and lower than normal grip.", out error);
            if (massKilograms <= 0f)
                return Fail("Vehicle mass must be positive.", out error);
            error = string.Empty;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
