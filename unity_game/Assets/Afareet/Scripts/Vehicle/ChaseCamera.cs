using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        private const int OcclusionHitCapacity = 16;

        public Transform Target { get; private set; }
        private readonly RaycastHit[] occlusionHits = new RaycastHit[OcclusionHitCapacity];
        private ChaseCameraConfig config;
        private Camera racingCamera;

        private void Awake() => racingCamera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (Target == null || config == null) return;

            var pivot = Target.position + Vector3.up * config.lookHeight;
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

            var car = Target.GetComponent<ArcadeCarController>();
            if (car != null)
                racingCamera.fieldOfView = Mathf.Lerp(
                    racingCamera.fieldOfView,
                    car.NitroActive ? config.nitroFieldOfView : config.normalFieldOfView,
                    Time.deltaTime * config.fieldOfViewDamping);
        }

        public void Configure(Transform target, ChaseCameraConfig cameraConfig)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (cameraConfig == null) throw new System.ArgumentNullException(nameof(cameraConfig));
            if (!cameraConfig.IsValid(out var error))
                throw new System.ArgumentException(error, nameof(cameraConfig));

            Target = target;
            config = cameraConfig;
            racingCamera.fieldOfView = config.normalFieldOfView;

            var pivot = Target.position + Vector3.up * config.lookHeight;
            transform.position = ResolveOcclusion(pivot, Target.TransformPoint(config.offset));
            var initialLookAt = pivot + Target.forward * config.lookAheadMeters;
            var initialDirection = initialLookAt - transform.position;
            if (initialDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(initialDirection.normalized, Vector3.up);
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

            var resolvedDistance = Mathf.Clamp(
                nearestDistance - config.collisionPadding,
                config.minimumOcclusionDistance,
                desiredDistance);
            return pivot + direction * resolvedDistance;
        }
    }
}
