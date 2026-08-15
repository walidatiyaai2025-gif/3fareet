using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraPresentationPulse : MonoBehaviour
    {
        private Camera racingCamera;
        private ArcadeCarController car;
        private float presentationOffset;

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
            var targetOffset = speed01 * 3f + (car.NitroActive ? 5f : 0f) + (car.IsDrifting ? 1.5f : 0f);
            presentationOffset = Mathf.Lerp(presentationOffset, targetOffset, Time.deltaTime * 5f);

            // ChaseCamera owns the baseline FOV. This component only adds a presentation offset.
            var baselineFov = car.NitroActive ? racingCamera.fieldOfView : Mathf.Min(racingCamera.fieldOfView, 72f);
            racingCamera.fieldOfView = Mathf.Clamp(baselineFov + presentationOffset * Time.deltaTime * 6f, 55f, 82f);

            // Do not modify transform position here: ChaseCamera remains the sole owner of camera motion.
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
