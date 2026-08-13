using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraPresentationPulse : MonoBehaviour
    {
        private Camera racingCamera;
        private ArcadeCarController car;
        private float baseFov;
        private Vector3 baseLocalPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var hook = new GameObject("AFAREET_CAMERA_PRESENTATION_HOOK");
            DontDestroyOnLoad(hook);
            hook.AddComponent<CameraPresentationInstaller>();
        }

        private void Awake()
        {
            racingCamera = GetComponent<Camera>();
            baseFov = racingCamera.fieldOfView;
            baseLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (car == null)
            {
                var chase = GetComponent<ChaseCamera>();
                if (chase != null && chase.Target != null)
                    car = chase.Target.GetComponent<ArcadeCarController>();
                if (car == null) return;
            }

            var speed01 = Mathf.Clamp01(Mathf.Abs(car.SpeedKph) / 180f);
            var nitroBoost = car.NitroActive ? 5f : 0f;
            var driftBoost = car.IsDrifting ? 1.5f : 0f;
            var targetFov = baseFov + speed01 * 3f + nitroBoost + driftBoost;
            racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, targetFov, Time.deltaTime * 4f);

            var shake = Vector3.zero;
            if (car.NitroActive && speed01 > .55f)
            {
                var wave = Mathf.Sin(Time.time * 31f) * .012f * speed01;
                shake = new Vector3(wave, -wave * .35f, 0f);
            }
            transform.localPosition = Vector3.Lerp(transform.localPosition, baseLocalPosition + shake, Time.deltaTime * 12f);
        }

        private sealed class CameraPresentationInstaller : MonoBehaviour
        {
            private void Update()
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var camera in cameras)
                {
                    if (camera.GetComponent<ChaseCamera>() == null) continue;
                    if (camera.GetComponent<CameraPresentationPulse>() == null)
                        camera.gameObject.AddComponent<CameraPresentationPulse>();
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
