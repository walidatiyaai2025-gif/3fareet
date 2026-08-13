using UnityEngine;

namespace Afareet.Vehicle
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target { get; set; }
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.4f, -8.5f);
        [SerializeField] private float positionDamping = 7f;
        [SerializeField] private float rotationDamping = 9f;
        private Camera racingCamera;

        private void Awake() => racingCamera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (Target == null) return;
            var desired = Target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionDamping * Time.deltaTime));
            var lookAt = Target.position + Target.forward * 8f + Vector3.up * 1.2f;
            var rotation = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-rotationDamping * Time.deltaTime));

            var car = Target.GetComponent<ArcadeCarController>();
            if (car != null)
                racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, car.NitroActive ? 78f : 65f, Time.deltaTime * 5f);
        }
    }
}
