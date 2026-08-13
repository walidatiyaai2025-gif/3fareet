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
        private float countdown;
        private bool racersReleased;

        public float RaceTime { get; private set; }
        public bool IsStarted { get; private set; }
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
            player.AcceptsPlayerInput = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
        }

        public void RegisterRival(ArcadeCarController rival)
        {
            rivals.Add(rival);
            rival.GetComponent<Rigidbody>().isKinematic = true;
            var ai = rival.GetComponent<AiRacer>();
            if (ai != null) ai.enabled = false;
        }

        public void StartRace()
        {
            if (IsStarted) return;
            IsStarted = true;
            countdown = 3.99f;
            RaceTime = 0f;
        }

        private void Update()
        {
            if (!IsStarted) return;
            if (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 1f) ReleaseRacers();
                return;
            }
            RaceTime += Time.deltaTime;
        }

        private void ReleaseRacers()
        {
            if (racersReleased) return;
            racersReleased = true;
            player.GetComponent<Rigidbody>().isKinematic = false;
            player.AcceptsPlayerInput = true;
            foreach (var rival in rivals)
            {
                rival.GetComponent<Rigidbody>().isKinematic = false;
                var ai = rival.GetComponent<AiRacer>();
                if (ai != null) ai.enabled = true;
            }
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
