using UnityEngine;

namespace Afareet.Vehicle
{
    [DefaultExecutionOrder(1000)]
    public sealed class CameraObstructionPass : MonoBehaviour
    {
        private Camera targetCamera;
        private ChaseCamera chase;
        private bool correctionLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET CAMERA OBSTRUCTION PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CameraObstructionPass>();
        }

        private void LateUpdate()
        {
            if (!ResolveCamera()) return;
            var target = chase.Target;
            if (target == null) return;

            // ChaseCamera is the sole owner of world-occlusion sphere casts. This late
            // safety pass must never run a second obstruction solve that can compress
            // the camera inside the Hero after ChaseCamera already produced a safe pose.
            var focus = chase.FocusPoint;
            var delta = targetCamera.transform.position - focus;
            var distance = delta.magnitude;
            var minimumDistance = chase.MinimumBodyClearanceDistance;
            if (minimumDistance <= 0f || distance >= minimumDistance - .001f) return;

            var direction = distance > .001f
                ? delta / distance
                : (-target.forward + Vector3.up * .2f).normalized;
            targetCamera.transform.position = focus + direction * minimumDistance;

            if (correctionLogged) return;
            correctionLogged = true;
            Debug.Log(
                $"AFAREET_CAMERA_BODY_CLEARANCE_RECOVERED previous={distance:F2}m " +
                $"minimum={minimumDistance:F2}m postPassClamp=true secondOcclusionSolve=false");
        }

        private bool ResolveCamera()
        {
            if (targetCamera != null && chase != null) return true;
            targetCamera = Camera.main;
            if (targetCamera == null) return false;
            chase = targetCamera.GetComponent<ChaseCamera>();
            return chase != null;
        }
    }
}
