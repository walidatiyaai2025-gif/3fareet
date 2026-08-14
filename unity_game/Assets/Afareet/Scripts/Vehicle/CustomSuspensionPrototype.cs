using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CustomSuspensionPrototype : MonoBehaviour
    {
        [Header("Prototype only - disabled by default")]
        [SerializeField] private bool applyForces;
        [SerializeField, Min(.05f)] private float restLength = .42f;
        [SerializeField, Min(1f)] private float springStrength = 18500f;
        [SerializeField, Min(0f)] private float damperStrength = 2400f;
        [SerializeField] private Vector3 halfTrackAndWheelbase = new(.82f, 0f, 1.35f);

        private Rigidbody body;
        private readonly Vector3[] localProbePoints = new Vector3[4];

        public int GroundedProbeCount { get; private set; }
        public float AverageCompression01 { get; private set; }
        public float PeakSpringForce { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            RebuildProbePoints();
        }

        private void OnValidate() => RebuildProbePoints();

        private void FixedUpdate()
        {
            if (body == null) return;

            GroundedProbeCount = 0;
            AverageCompression01 = 0f;
            PeakSpringForce = 0f;

            var compressionSum = 0f;
            for (var i = 0; i < localProbePoints.Length; i++)
            {
                var worldPoint = transform.TransformPoint(localProbePoints[i]);
                if (!Physics.Raycast(
                        worldPoint,
                        -transform.up,
                        out var hit,
                        restLength,
                        ~0,
                        QueryTriggerInteraction.Ignore))
                    continue;

                GroundedProbeCount++;
                var compression01 = Mathf.Clamp01(1f - hit.distance / restLength);
                compressionSum += compression01;

                var pointVelocity = body.GetPointVelocity(worldPoint);
                var verticalSpeed = Vector3.Dot(pointVelocity, transform.up);
                var springForce = EvaluateSpringForce(
                    compression01,
                    verticalSpeed,
                    springStrength,
                    damperStrength
                );
                PeakSpringForce = Mathf.Max(PeakSpringForce, springForce);

                if (applyForces)
                    body.AddForceAtPosition(transform.up * springForce, worldPoint, ForceMode.Force);
            }

            if (GroundedProbeCount > 0)
                AverageCompression01 = compressionSum / GroundedProbeCount;
        }

        public static float EvaluateSpringForce(
            float compression01,
            float verticalSpeed,
            float spring,
            float damper)
        {
            var force = Mathf.Clamp01(compression01) * Mathf.Max(0f, spring)
                - verticalSpeed * Mathf.Max(0f, damper);
            return Mathf.Max(0f, force);
        }

        private void RebuildProbePoints()
        {
            var x = Mathf.Abs(halfTrackAndWheelbase.x);
            var z = Mathf.Abs(halfTrackAndWheelbase.z);
            localProbePoints[0] = new Vector3(-x, 0f, z);
            localProbePoints[1] = new Vector3(x, 0f, z);
            localProbePoints[2] = new Vector3(-x, 0f, -z);
            localProbePoints[3] = new Vector3(x, 0f, -z);
        }
    }
}
