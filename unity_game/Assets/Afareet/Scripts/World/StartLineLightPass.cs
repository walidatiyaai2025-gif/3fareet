using UnityEngine;

namespace Afareet.World
{
    public sealed class StartLineLightPass : MonoBehaviour
    {
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var host = new GameObject("AFAREET START LINE LIGHT PASS");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<StartLineLightPass>();
        }

        private void Update()
        {
            if (built) return;
            var waypoint = GameObject.Find("Waypoint 00");
            if (waypoint == null) return;

            var anchor = waypoint.transform;
            AddLight(anchor, -6.2f, new Color(.52f, .02f, 1f));
            AddLight(anchor, 6.2f, new Color(.02f, .82f, 1f));
            built = true;
        }

        private static void AddLight(Transform anchor, float side, Color color)
        {
            var light = new GameObject("Start Spirit Light").AddComponent<Light>();
            light.transform.position = anchor.position + anchor.right * side + Vector3.up * 3.2f;
            light.type = LightType.Point;
            light.color = color;
            light.range = 9f;
            light.intensity = 4.2f;
        }
    }
}
