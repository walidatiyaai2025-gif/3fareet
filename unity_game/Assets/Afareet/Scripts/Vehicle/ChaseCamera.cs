using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target { get; private set; }
        private ChaseCameraConfig config;
        private Camera racingCamera;

        private void Awake() => racingCamera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (Target == null || config == null) return;
            var desired = Target.TransformPoint(config.offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-config.positionDamping * Time.deltaTime));
            var lookAt = Target.position + Target.forward * config.lookAheadMeters + Vector3.up * 1.2f;
            var rotation = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-config.rotationDamping * Time.deltaTime));

            var car = Target.GetComponent<ArcadeCarController>();
            if (car != null)
                racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, car.NitroActive ? config.nitroFieldOfView : config.normalFieldOfView, Time.deltaTime * config.fieldOfViewDamping);
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
        }
    }
}
