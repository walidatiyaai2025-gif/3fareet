using System;
using System.Collections.Generic;
using Afareet.CareerRuntime;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Core
{
    public sealed class CareerTrackRuntimeController : ICareerTrackRuntime
    {
        private readonly Transform host;
        private readonly ArcadeCarController player;
        private readonly IReadOnlyList<ArcadeCarController> rivals;
        private readonly RaceDirector race;
        private GameObject activeRoot;

        public string ActiveTrackId { get; private set; }

        public CareerTrackRuntimeController(
            Transform trackHost,
            GameObject initialTrackRoot,
            string initialTrackId,
            ArcadeCarController playerCar,
            IReadOnlyList<ArcadeCarController> registeredRivals,
            RaceDirector raceDirector)
        {
            host = trackHost != null ? trackHost : throw new ArgumentNullException(nameof(trackHost));
            activeRoot = initialTrackRoot != null ? initialTrackRoot : throw new ArgumentNullException(nameof(initialTrackRoot));
            player = playerCar != null ? playerCar : throw new ArgumentNullException(nameof(playerCar));
            rivals = registeredRivals ?? throw new ArgumentNullException(nameof(registeredRivals));
            race = raceDirector != null ? raceDirector : throw new ArgumentNullException(nameof(raceDirector));
            ActiveTrackId = CairoCareerTrackCatalog.Resolve(initialTrackId).Id;
        }

        public bool ApplyTrack(string trackId)
        {
            var spec = CairoCareerTrackCatalog.Resolve(trackId);
            if (StringComparer.Ordinal.Equals(ActiveTrackId, spec.Id))
                return false;
            if (race.Phase == RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing)
                throw new InvalidOperationException("Career TrackId cannot change during countdown or active racing.");

            var build = CairoCareerTrackBuilder.Build(host, spec);
            try
            {
                race.Configure(player, build.Track);
                RebindAiPaths(build.Track);
            }
            catch
            {
                DestroyRuntimeRoot(build.Root);
                throw;
            }

            var previousRoot = activeRoot;
            activeRoot = build.Root;
            ActiveTrackId = spec.Id;
            if (previousRoot != null)
            {
                previousRoot.SetActive(false);
                DestroyRuntimeRoot(previousRoot);
            }

            Debug.Log($"AFAREET_CAREER_TRACK_APPLIED id={ActiveTrackId} waypoints={build.Track.Waypoints.Count} signature={spec.DeterministicSignature}");
            return true;
        }

        private void RebindAiPaths(TrackRuntime track)
        {
            for (var index = 0; index < rivals.Count; index++)
            {
                var rival = rivals[index];
                if (rival == null) continue;
                var ai = rival.GetComponent<AiRacer>();
                if (ai != null)
                    ai.Configure(track.Waypoints, index);
            }
        }

        private static void DestroyRuntimeRoot(GameObject root)
        {
            if (root == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(root);
            else
                UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
