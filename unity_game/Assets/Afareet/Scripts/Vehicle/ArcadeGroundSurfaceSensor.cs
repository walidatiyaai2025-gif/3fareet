using System;
using UnityEngine;

namespace Afareet.Vehicle
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class ArcadeGroundSurfaceSensor : MonoBehaviour
    {
        private const int ProbeCapacity = 8;
        private readonly RaycastHit[] probeHits = new RaycastHit[ProbeCapacity];

        [SerializeField, Min(0f)] private float rayOriginOffset = 0.35f;
        [SerializeField, Min(0.05f)] private float rayDistance = 1.4f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        public bool IsGrounded { get; private set; }
        public ArcadeSurfaceKind CurrentSurface { get; private set; } = ArcadeSurfaceKind.Asphalt;
        public Collider GroundCollider { get; private set; }
        public float GroundDistance { get; private set; } = float.PositiveInfinity;

        private void FixedUpdate()
        {
            ProbeNow();
        }

        public void ConfigureProbe(float originOffset, float distance, LayerMask mask)
        {
            if (originOffset < 0f)
                throw new ArgumentOutOfRangeException(nameof(originOffset));
            if (distance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(distance));

            rayOriginOffset = originOffset;
            rayDistance = distance;
            surfaceMask = mask;
        }

        public void ProbeNow()
        {
            var origin = transform.position + Vector3.up * rayOriginOffset;
            var count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                probeHits,
                rayDistance,
                surfaceMask,
                QueryTriggerInteraction.Ignore);

            RaycastHit best = default;
            var found = false;
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = probeHits[i];
                if (hit.collider == null || IsOwnCollider(hit.collider))
                    continue;
                if (hit.distance >= bestDistance)
                    continue;

                best = hit;
                bestDistance = hit.distance;
                found = true;
            }

            var previousGroundCollider = GroundCollider;
            IsGrounded = found;
            GroundCollider = found ? best.collider : null;
            GroundDistance = found ? bestDistance : float.PositiveInfinity;

            // Authored race-surface metadata is static. The non-alloc grounding probe still
            // executes each physics tick, but marker/name classification only needs to rerun
            // when the contacted collider changes. Leaving ground sets GroundCollider to null,
            // so landing back on the same collider correctly triggers classification again.
            if (found && previousGroundCollider != GroundCollider)
                CurrentSurface = Classify(GroundCollider);
        }

        public static ArcadeSurfaceKind Classify(Collider collider)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            var marker = collider.GetComponentInParent<ArcadeSurfaceMarker>();
            if (marker != null)
                return marker.SurfaceKind;

            var objectName = collider.gameObject.name;
            if (Contains(objectName, "boost"))
                return ArcadeSurfaceKind.Boost;
            if (Contains(objectName, "slippery") || Contains(objectName, "oil") || Contains(objectName, "ice"))
                return ArcadeSurfaceKind.Slippery;
            if (Contains(objectName, "road") || Contains(objectName, "asphalt") || Contains(objectName, "rune"))
                return ArcadeSurfaceKind.Asphalt;
            if (Contains(objectName, "desert") || Contains(objectName, "sand") || Contains(objectName, "ground"))
                return ArcadeSurfaceKind.OffRoad;

            // Unknown physical ground is conservative: treat it as off-road until explicitly marked.
            return ArcadeSurfaceKind.OffRoad;
        }

        private bool IsOwnCollider(Collider collider)
        {
            var hitTransform = collider.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        private static bool Contains(string value, string fragment)
        {
            return value != null && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
