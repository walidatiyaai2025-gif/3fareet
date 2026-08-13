using System.Collections;
using UnityEngine;

namespace Afareet.World
{
    public sealed class TrackSpectacleBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("3FAREET_VISUAL_BOOTSTRAP");
            DontDestroyOnLoad(host);
            host.AddComponent<TrackSpectacleBootstrap>();
        }

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 240; frame++)
            {
                var first = GameObject.Find("Waypoint 00");
                if (first != null)
                {
                    var track = new TrackRuntime();
                    for (var i = 0; i < 72; i++)
                    {
                        var waypoint = GameObject.Find($"Waypoint {i:00}");
                        if (waypoint == null) break;
                        track.Waypoints.Add(waypoint.transform);
                    }

                    if (track.Waypoints.Count > 0)
                    {
                        gameObject.AddComponent<TrackSpectacle>().Build(track);
                        yield break;
                    }
                }
                yield return null;
            }
        }
    }
}
