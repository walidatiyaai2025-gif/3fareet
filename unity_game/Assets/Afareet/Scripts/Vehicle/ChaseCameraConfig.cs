using UnityEngine;

namespace Afareet.Vehicle
{
    [CreateAssetMenu(menuName = "Afareet/Camera/Chase Camera Config", fileName = "ChaseCameraConfig")]
    public sealed class ChaseCameraConfig : ScriptableObject
    {
        public Vector3 offset = new(0f, 4.4f, -8.5f);
        [Min(0.1f)] public float positionDamping = 7f;
        [Min(0.1f)] public float rotationDamping = 9f;
        [Min(1f)] public float lookAheadMeters = 8f;
        [Range(30f, 110f)] public float normalFieldOfView = 65f;
        [Range(30f, 110f)] public float nitroFieldOfView = 78f;
        [Min(0.1f)] public float fieldOfViewDamping = 5f;

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
