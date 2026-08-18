using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        private const int OcclusionHitCapacity = 16;
        private const float MinimumPlayableBodyClearance = 2.8f;

        public Transform Target { get; private set; }
        public float MinimumBodyClearanceDistance => minimumBodyClearanceDistance;
        public Vector3 FocusPoint =>
            Target == null || config == null
                ? transform.position
                : Target.position + Vector3.up * config.lookHeight;

        private readonly RaycastHit[] occlusionHits = new RaycastHit[OcclusionHitCapacity];
        private ChaseCameraConfig config;
        private Camera racingCamera;
        private ArcadeCarController targetCar;
        private float minimumBodyClearanceDistance;

        private void Awake() => racingCamera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (Target == null || config == null) return;

            var pivot = FocusPoint;
            var desired = ResolveOcclusion(pivot, Target.TransformPoint(config.offset));
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-config.positionDamping * Time.deltaTime));

            var lookAt = pivot + Target.forward * config.lookAheadMeters;
            var direction = lookAt - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    rotation,
                    1f - Mathf.Exp(-config.rotationDamping * Time.deltaTime));
            }

            // Normal runtime composition binds the vehicle before Configure(). Keep a lazy
            // fallback for unusual initialization order, but avoid a GetComponent lookup on
            // every rendered frame once the target controller has been resolved.
            if (targetCar == null)
                targetCar = Target.GetComponent<ArcadeCarController>();
            if (targetCar != null)
                racingCamera.fieldOfView = Mathf.Lerp(
                    racingCamera.fieldOfView,
                    targetCar.NitroActive ? config.nitroFieldOfView : config.normalFieldOfView,
                    Time.deltaTime * config.fieldOfViewDamping);
        }

        public void Configure(Transform target, ChaseCameraConfig cameraConfig)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (cameraConfig == null) throw new System.ArgumentNullException(nameof(cameraConfig));
            if (!cameraConfig.IsValid(out var error))
                throw new System.ArgumentException(error, nameof(cameraConfig));

            Target = target;
            targetCar = target.GetComponent<ArcadeCarController>();
            config = cameraConfig;
            racingCamera.fieldOfView = config.normalFieldOfView;
            minimumBodyClearanceDistance = CalculateMinimumBodyClearance(Target, config, out var clearanceSource);

            var pivot = FocusPoint;
            transform.position = ResolveOcclusion(pivot, Target.TransformPoint(config.offset));
            var initialLookAt = pivot + Target.forward * config.lookAheadMeters;
            var initialDirection = initialLookAt - transform.position;
            if (initialDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(initialDirection.normalized, Vector3.up);

            Debug.Log(
                $"AFAREET_CAMERA_BODY_CLEARANCE_ACTIVE minimum={minimumBodyClearanceDistance:F2}m " +
                $"source={clearanceSource} occlusionOwner=ChaseCamera postPassMayNotCompress=true");
        }

        private Vector3 ResolveOcclusion(Vector3 pivot, Vector3 desiredPosition)
        {
            var delta = desiredPosition - pivot;
            var desiredDistance = delta.magnitude;
            if (desiredDistance <= 0.001f)
                return desiredPosition;

            var direction = delta / desiredDistance;
            var hitCount = Physics.SphereCastNonAlloc(
                pivot,
                config.collisionRadius,
                direction,
                occlusionHits,
                desiredDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            var nearestDistance = desiredDistance;
            var occluded = false;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = occlusionHits[i];
                if (hit.collider == null || hit.transform == null)
                    continue;
                if (hit.transform == Target || hit.transform.IsChildOf(Target))
                    continue;
                if (hit.distance <= 0f || hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
                occluded = true;
            }

            if (!occluded)
                return desiredPosition;

            var effectiveMinimumDistance = Mathf.Max(
                config.minimumOcclusionDistance,
                minimumBodyClearanceDistance);
            var resolvedDistance = Mathf.Clamp(
                nearestDistance - config.collisionPadding,
                Mathf.Min(effectiveMinimumDistance, desiredDistance),
                desiredDistance);
            return pivot + direction * resolvedDistance;
        }

        private static float CalculateMinimumBodyClearance(
            Transform target,
            ChaseCameraConfig cameraConfig,
            out string source)
        {
            var pivot = target.position + Vector3.up * cameraConfig.lookHeight;
            var desired = target.TransformPoint(cameraConfig.offset);
            var desiredDelta = desired - pivot;
            var desiredDirection = desiredDelta.sqrMagnitude > .0001f
                ? desiredDelta.normalized
                : -target.forward;

            if (TryCalculateCombinedBounds(target, out var bounds, out source))
            {
                var extents = bounds.extents;
                var projectedExtent =
                    Mathf.Abs(desiredDirection.x) * extents.x +
                    Mathf.Abs(desiredDirection.y) * extents.y +
                    Mathf.Abs(desiredDirection.z) * extents.z;
                var centerProjection = Vector3.Dot(bounds.center - pivot, desiredDirection);
                var rearProjection = Mathf.Max(0f, centerProjection + projectedExtent);
                return Mathf.Max(
                    MinimumPlayableBodyClearance,
                    cameraConfig.minimumOcclusionDistance,
                    rearProjection + cameraConfig.collisionRadius + cameraConfig.collisionPadding);
            }

            source = "playable-floor";
            return Mathf.Max(MinimumPlayableBodyClearance, cameraConfig.minimumOcclusionDistance);
        }

        private static bool TryCalculateCombinedBounds(
            Transform target,
            out Bounds combined,
            out string source)
        {
            combined = default;
            var hasBounds = false;

            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;
                var bounds = renderer.bounds;
                if (bounds.size.sqrMagnitude <= .0001f)
                    continue;

                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(bounds);
                }
            }

            if (hasBounds)
            {
                source = "renderer-bounds";
                return true;
            }

            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || collider.isTrigger)
                    continue;
                var bounds = collider.bounds;
                if (bounds.size.sqrMagnitude <= .0001f)
                    continue;

                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(bounds);
                }
            }

            source = hasBounds ? "collider-bounds" : "none";
            return hasBounds;
        }
    }
}