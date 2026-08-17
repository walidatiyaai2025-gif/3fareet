using UnityEngine;

namespace Afareet.Vehicle
{
    [CreateAssetMenu(menuName = "Afareet/Camera/Chase Camera Config", fileName = "ChaseCameraConfig")]
    public sealed class ChaseCameraConfig : ScriptableObject
    {
        public Vector3 offset = new(0f, 3.25f, -7.2f);
        [Min(0.1f)] public float positionDamping = 8f;
        [Min(0.1f)] public float rotationDamping = 10f;
        [Min(1f)] public float lookAheadMeters = 6f;
        [Min(0.1f)] public float lookHeight = 1.25f;
        [Range(30f, 110f)] public float normalFieldOfView = 68f;
        [Range(30f, 110f)] public float nitroFieldOfView = 80f;
        [Min(0.1f)] public float fieldOfViewDamping = 6f;
        [Min(0.05f)] public float collisionRadius = 0.35f;
        [Min(0.01f)] public float collisionPadding = 0.25f;
        [Min(0.2f)] public float minimumOcclusionDistance = 1.1f;

        public bool IsValid(out string error)
        {
            if (offset.z >= 0f)
            {
                error = "Chase camera offset must remain behind the vehicle.";
                return false;
            }
            if (positionDamping <= 0f || rotationDamping <= 0f || fieldOfViewDamping <= 0f)
            {
                error = "Camera damping values must be positive.";
                return false;
            }
            if (lookHeight <= 0f || collisionRadius <= 0f || collisionPadding <= 0f || minimumOcclusionDistance <= 0f)
            {
                error = "Camera framing and occlusion values must be positive.";
                return false;
            }
            if (nitroFieldOfView < normalFieldOfView)
            {
                error = "Nitro FOV must be greater than or equal to normal FOV.";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }
}
