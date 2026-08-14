using UnityEngine;

namespace Afareet.Vehicle
{
    public static class CameraMotionSettings
    {
        private const string Key = "afareet.reduce_camera_motion";

        public static bool ReducedMotion => PlayerPrefs.GetInt(Key, 0) == 1;

        public static void SetReducedMotion(bool enabled)
        {
            PlayerPrefs.SetInt(Key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
