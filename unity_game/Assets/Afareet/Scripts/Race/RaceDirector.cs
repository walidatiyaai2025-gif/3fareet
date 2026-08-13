using System.Collections.Generic;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Race
{
    public sealed class RaceDirector : MonoBehaviour
    {
        private readonly List<ArcadeCarController> rivals = new();
        private ArcadeCarController player;
        private TrackRuntime track;
        private float countdown = 3.99f;

        public float RaceTime { get; private set; }
        public string CountdownText => countdown > 1f ? Mathf.CeilToInt(countdown - 1f).ToString() : countdown > 0f ? "GO!" : string.Empty;
        public int Position
        {
            get
            {
                if (player == null || track == null) return 1;
                var playerProgress = Progress(player.transform.position);
                var ahead = 0;
                foreach (var rival in rivals) if (Progress(rival.transform.position) > playerProgress) ahead++;
                return ahead + 1;
            }
        }

        public void Configure(ArcadeCarController playerCar, TrackRuntime runtimeTrack)
        {
            player = playerCar;
            track = runtimeTrack;
        }

        public void RegisterRival(ArcadeCarController rival) => rivals.Add(rival);

        private void Update()
        {
            if (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                player.AcceptsPlayerInput = countdown <= 1f;
                return;
            }
            RaceTime += Time.deltaTime;
        }

        private float Progress(Vector3 position)
        {
            var nearest = 0;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < track.Waypoints.Count; i++)
            {
                var distance = (track.Waypoints[i].position - position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = i;
                nearestDistance = distance;
            }
            return nearest / (float)track.Waypoints.Count;
        }
    }
}
