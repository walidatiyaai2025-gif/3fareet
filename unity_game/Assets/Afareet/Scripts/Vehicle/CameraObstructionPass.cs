using UnityEngine;

namespace Afareet.Vehicle
{
    [DefaultExecutionOrder(1000)]
    public sealed class CameraObstructionPass : MonoBehaviour
    {
        private Camera targetCamera;
        private ChaseCamera chase;
        private const float ProbeRadius = .28f;
        private const float SurfacePadding = .18f;

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

            var focus = target.position + Vector3.up * 1.2f;
            var desired = targetCamera.transform.position;
            var delta = desired - focus;
            var distance = delta.magnitude;
            if (distance <= .25f) return;

            var direction = delta / distance;
            var hits = Physics.SphereCastAll(focus, ProbeRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            var nearest = distance;
            var found = false;
            for (var i = 0; i < hits.Length; i++)
            {
                var hitTransform = hits[i].collider == null ? null : hits[i].collider.transform;
                if (hitTransform == null || hitTransform.IsChildOf(target)) continue;
                if (hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                found = true;
            }

            if (!found) return;
            var safeDistance = Mathf.Max(.35f, nearest - SurfacePadding);
            targetCamera.transform.position = focus + direction * safeDistance;
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
