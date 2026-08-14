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
        private readonly RaycastHit[] hits = new RaycastHit[6];
        private Rigidbody ownerBody;

        public VehicleSurface CurrentSurface { get; private set; } = VehicleSurface.Asphalt;
        public bool IsGrounded { get; private set; }
        public float AccelerationMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .72f : 1f;
        public float GripMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .62f : 1f;
        public float SpeedMultiplier => CurrentSurface == VehicleSurface.OffRoad ? .78f : 1f;

        private void Awake() => ownerBody = GetComponentInParent<Rigidbody>();

        private void FixedUpdate()
        {
            var origin = transform.position + transform.up * .35f;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                -transform.up,
                hits,
                ProbeDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            Collider groundCollider = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.attachedRigidbody == ownerBody) continue;
                if (hit.distance >= nearestDistance) continue;
                nearestDistance = hit.distance;
                groundCollider = hit.collider;
            }

            if (groundCollider == null)
            {
                IsGrounded = false;
                CurrentSurface = VehicleSurface.Asphalt;
                return;
            }

            IsGrounded = true;
            CurrentSurface = Classify(groundCollider);
        }

        private static VehicleSurface Classify(Collider collider)
        {
            if (collider == null) return VehicleSurface.Asphalt;

            var materialName = collider.sharedMaterial == null
                ? string.Empty
                : collider.sharedMaterial.name;
            var combined = (collider.gameObject.name + " " + materialName).ToLowerInvariant();

            return combined.Contains("sand")
                || combined.Contains("desert")
                || combined.Contains("dirt")
                || combined.Contains("offroad")
                || combined.Contains("shoulder")
                    ? VehicleSurface.OffRoad
                    : VehicleSurface.Asphalt;
        }
    }
}
