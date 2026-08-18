using System;
using System.Collections;
using Afareet.CareerRuntime;
using Afareet.Progression;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Afareet.Tests.PlayMode
{
    public sealed class CareerSelectedEventActivationPlayModeTests
    {
        private sealed class MemoryStorage : ICareerProgressStorage
        {
            public string Payload;
            public bool TryRead(out string payload)
            {
                payload = Payload;
                return Payload != null;
            }
            public void Write(string payload) => Payload = payload;
            public void Clear() => Payload = null;
        }

        private sealed class ReconfiguringTrackRuntime : ICareerTrackRuntime
        {
            private readonly RaceDirector race;
            private readonly ArcadeCarController player;
            private readonly TrackRuntime track;

            public string ActiveTrackId { get; private set; }
            public int ApplyCount { get; private set; }
            public int ForcedRebuildCount { get; private set; }

            public ReconfiguringTrackRuntime(
                RaceDirector director,
                ArcadeCarController playerCar,
                TrackRuntime runtimeTrack,
                string initialTrackId)
            {
                race = director;
                player = playerCar;
                track = runtimeTrack;
                ActiveTrackId = initialTrackId;
            }

            public bool ApplyTrack(string trackId, bool forceRebuild = false)
            {
                if (string.IsNullOrWhiteSpace(trackId))
                    throw new ArgumentException(nameof(trackId));

                var changed = !StringComparer.Ordinal.Equals(ActiveTrackId, trackId);
                if (!changed && !forceRebuild)
                    return false;
                if (race.Phase == RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing)
                    throw new InvalidOperationException("Unsafe test TrackId mutation.");

                ApplyCount++;
                ActiveTrackId = trackId;
                if (forceRebuild)
                {
                    ForcedRebuildCount++;
                    race.Configure(player, track);
                }
                return true;
            }
        }

        private GameObject root;
        private GameObject playerObject;
        private ArcadeCarController player;
        private RaceDirector race;
        private RacePerformanceMetricsTracker performance;
        private CareerGameSession career;
        private TrackRuntime track;
        private ReconfiguringTrackRuntime trackRuntime;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("CAREER SELECTED EVENT TEST ROOT");
            playerObject = new GameObject("CAREER SELECTED EVENT TEST PLAYER");
            playerObject.AddComponent<Rigidbody>();
            player = playerObject.AddComponent<ArcadeCarController>();

            track = BuildTrack();
            race = root.AddComponent<RaceDirector>();
            race.Configure(player, track);
            performance = root.AddComponent<RacePerformanceMetricsTracker>();
            performance.Configure(player, race);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (root != null) Object.Destroy(root);
            if (playerObject != null) Object.Destroy(playerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedSelection_IsRejectedWithoutChangingActiveRuntime()
        {
            ConfigureCareer(SeedProfile(completedFirstNode: true, stars: 3));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r02"));
            var previousTrack = career.ActiveTrackId;
            var previousChallenge = race.ChallengeConfiguration;

            career.SelectCareerNode("c01_r03");
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(career.TryActivateSelectedEvent(), Is.False);

            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r02"));
            Assert.That(career.ActiveTrackId, Is.EqualTo(previousTrack));
            Assert.That(race.ChallengeConfiguration.ActiveRivalCount, Is.EqualTo(previousChallenge.ActiveRivalCount));
            Assert.That(trackRuntime.ApplyCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletedSelection_CanBecomeActiveForReplayWhileReady()
        {
            ConfigureCareer(SeedProfile(completedFirstNode: true, stars: 3));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r02"));

            career.SelectCareerNode("c01_r01");
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Completed));
            Assert.That(career.TryActivateSelectedEvent(), Is.True);

            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Ready));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r01"));
            Assert.That(career.Progress.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(career.ActiveTrackId, Is.EqualTo("cairo_corniche_night"));
            Assert.That(race.RequestedActiveRivalCount, Is.EqualTo(3));
            Assert.That(trackRuntime.ApplyCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultsReplay_ForceRebuildsSameTrackBackToReadyWithoutAutoStart()
        {
            ConfigureCareer(CareerPlayerProfile.Empty());
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));

            CompleteCurrentRace();
            yield return null;
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(career.Progress.IsNodeCompleted("c01_r01"), Is.True);
            var claimedCoins = career.Profile.Coins;

            career.SelectCareerNode("c01_r01");
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Completed));
            Assert.That(career.TryActivateSelectedEvent(), Is.True);

            Assert.That(trackRuntime.ForcedRebuildCount, Is.EqualTo(1));
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Ready));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(career.Progress.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(career.Profile.Coins, Is.EqualTo(claimedCoins));
            Assert.That(career.LastSettlement, Is.Null);
            Assert.That(career.LastCoinRewardSettlement, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CountdownActivation_IsRejectedAfterSideEffectFreeBrowsing()
        {
            ConfigureCareer(SeedProfile(completedFirstNode: true, stars: 3));
            var activeBefore = career.ActiveDefinition.Node.Id;

            race.StartRace();
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            career.SelectCareerNode("c01_r01");
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r01"));
            Assert.That(career.TryActivateSelectedEvent(), Is.False);

            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo(activeBefore));
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(trackRuntime.ApplyCount, Is.Zero);
            yield return null;
        }

        private void ConfigureCareer(CareerPlayerProfile profile)
        {
            var storage = new MemoryStorage();
            if (profile != null && !ReferenceEquals(profile, CareerPlayerProfile.Empty()))
                storage.Payload = new CareerPlayerProfileCodec().Encode(profile);

            trackRuntime = new ReconfiguringTrackRuntime(
                race,
                player,
                track,
                "cairo_corniche_night");
            career = root.AddComponent<CareerGameSession>();
            career.Configure(
                player.GetComponent<RaceRoundController>(),
                race,
                performance,
                storage,
                trackRuntime,
                new PassiveCareerBossVehicleRuntime());
        }

        private static CareerPlayerProfile SeedProfile(bool completedFirstNode, int stars)
        {
            var completed = completedFirstNode ? new[] { "c01_r01" } : Array.Empty<string>();
            var rewardIds = completedFirstNode ? new[] { "career:c01_r01:reward:00" } : Array.Empty<string>();
            return new CareerPlayerProfile(
                new CareerProgress(CareerProgress.CurrentVersion, stars, completed, rewardIds),
                coins: completedFirstNode ? 250 : 0,
                spirit: completedFirstNode ? 5 : 0,
                unlockedVehicleIds: Array.Empty<string>());
        }

        private void CompleteCurrentRace()
        {
            race.StartRace();
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(round.AdvanceCountdown(3f), Is.True);
            var checkpoints = player.GetComponent<RacerCheckpointTracker>();
            Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(3), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));
        }

        private TrackRuntime BuildTrack()
        {
            var runtime = new TrackRuntime();
            var points = new[]
            {
                Vector3.zero,
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = new GameObject($"ActivationWaypoint-{index}");
                waypoint.transform.SetParent(root.transform, false);
                waypoint.transform.position = points[index];
                runtime.Waypoints.Add(waypoint.transform);
            }

            for (var index = 0; index < runtime.Waypoints.Count; index++)
            {
                var current = runtime.Waypoints[index];
                var next = runtime.Waypoints[(index + 1) % runtime.Waypoints.Count];
                current.rotation = Quaternion.LookRotation((next.position - current.position).normalized);
            }
            return runtime;
        }
    }
}
