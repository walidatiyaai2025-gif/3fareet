using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraPresentationPulse : MonoBehaviour
    {
        private Camera racingCamera;
        private ArcadeCarController car;
        private float baseFov;

        private void Awake()
        {
            racingCamera = GetComponent<Camera>();
            baseFov = racingCamera.fieldOfView;
        }

        private void Start()
        {
            var chase = GetComponent<ChaseCamera>();
            if (chase != null && chase.Target != null)
                car = chase.Target.GetComponent<ArcadeCarController>();
        }

        private void LateUpdate()
        {
            if (racingCamera == null || car == null) return;

            var speed01 = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 180f);
            var nitroBoost = car.NitroActive ? 5f : 0f;
            var driftBoost = car.IsDrifting ? 1.5f : 0f;
            var targetFov = baseFov + speed01 * 3f + nitroBoost + driftBoost;
            racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, targetFov, Time.deltaTime * 4f);

            if (car.NitroActive && speed01 > .55f)
            {
                var shake = Mathf.Sin(Time.time * 31f) * .012f * speed01;
                transform.localPosition += new Vector3(shake, -shake * .35f, 0f);
            }
        }
    }
}
