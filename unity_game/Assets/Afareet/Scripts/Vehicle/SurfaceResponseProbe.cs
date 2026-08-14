using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    public enum VehicleSurface
    {
        Asphalt,
        OffRoad
    }

    public sealed class SurfaceResponseProbe : MonoBehaviour
    {
        private const float ProbeDistance = 1.35f;

        public VehicleSurface CurrentSurface { get; private set; } = VehicleSurface.Asphalt;
        public bool IsGrounded { get; private set; }
        public float AccelerationMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .72f : 1f;
        public float GripMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .62f : 1f;
        public float SpeedMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .78f : 1f;

        private void FixedUpdate()
        {
            var origin = transform.position + transform.up * .35f;
            if (!Physics.Raycast(origin, -transform.up, out var hit, ProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                IsGrounded = false;
                CurrentSurface = VehicleSurface.Asphalt;
                return;
            }

            IsGrounded = true;
            CurrentSurface = Classify(hit.collider);
        }

        private static VehicleSurface Classify(Collider collider)
        {
            if (collider == null) return VehicleSurface.Asphalt;

            var materialName = collider.sharedMaterial == null
                ? string.Empty
                : collider.sharedMaterial.name;
            var combined = string.Concat(collider.gameObject.name, " ", materialName).ToLowerInvariant();

            return combined.Contains("sand", StringComparison.Ordinal)
                || combined.Contains("dirt", StringComparison.Ordinal)
                || combined.Contains("offroad", StringComparison.Ordinal)
                || combined.Contains("shoulder", StringComparison.Ordinal)
                    ? VehicleSurface.OffRoad
                    : VehicleSurface.Asphalt;
        }
    }
}
