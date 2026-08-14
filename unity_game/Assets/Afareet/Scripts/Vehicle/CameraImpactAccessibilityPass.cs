using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraImpactAccessibilityPass : MonoBehaviour
    {
        private const string ReducedMotionKey = "afareet.camera.reduced_motion";
        private Camera racingCamera;
        private CameraPresentationPulse presentation;
        private CrashResponseRelay crash;
        private float impactKick;

        public static bool ReducedMotion => PlayerPrefs.GetInt(ReducedMotionKey, 0) == 1;

        public static void SetReducedMotion(bool enabled)
        {
            PlayerPrefs.SetInt(ReducedMotionKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET CAMERA IMPACT INSTALLER");
            DontDestroyOnLoad(host);
            host.AddComponent<Installer>();
        }

        private void Awake()
        {
            racingCamera = GetComponent<Camera>();
            presentation = GetComponent<CameraPresentationPulse>();
        }

        private void Update()
        {
            BindCrashRelay();
            if (presentation != null) presentation.enabled = !ReducedMotion;
        }

        private void LateUpdate()
        {
            if (ReducedMotion)
            {
                impactKick = 0f;
                return;
            }

            if (impactKick <= 0f) return;
            racingCamera.fieldOfView = Mathf.Min(84f, racingCamera.fieldOfView + impactKick);
            impactKick = Mathf.MoveTowards(impactKick, 0f, Time.deltaTime * 18f);
        }

        private void BindCrashRelay()
        {
            if (crash != null) return;
            var chase = GetComponent<ChaseCamera>();
            if (chase == null || chase.Target == null) return;
            crash = chase.Target.GetComponent<CrashResponseRelay>();
            if (crash != null) crash.Impacted += OnImpact;
        }

        private void OnImpact(float speed, Vector3 point)
        {
            impactKick = Mathf.Lerp(.6f, 3.2f, Mathf.InverseLerp(7f, 32f, speed));
        }

        private void OnDestroy()
        {
            if (crash != null) crash.Impacted -= OnImpact;
        }

        private sealed class Installer : MonoBehaviour
        {
            private void Update()
            {
                foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (camera.GetComponent<ChaseCamera>() == null) continue;
                    if (camera.GetComponent<CameraImpactAccessibilityPass>() == null)
                        camera.gameObject.AddComponent<CameraImpactAccessibilityPass>();
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
