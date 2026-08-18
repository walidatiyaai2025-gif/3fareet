using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class CareerGameSessionPlayModeTests
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

        private readonly List<ArcadeCarController> rivals = new List<ArcadeCarController>();
        private GameObject root;
        private GameObject playerObject;
        private ArcadeCarController player;
        private RaceDirector race;
        private RacePerformanceMetricsTracker performance;
        private CareerGameSession career;
        private MemoryStorage storage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            rivals.Clear();
            root = new GameObject("CAREER SESSION TEST ROOT");
            playerObject = new GameObject("CAREER SESSION TEST PLAYER");
            playerObject.AddComponent<Rigidbody>();
            player = playerObject.AddComponent<ArcadeCarController>();

            race = root.AddComponent<RaceDirector>();
            RegisterTestRivals(3);
            race.Configure(player, BuildTrack());
            performance = root.AddComponent<RacePerformanceMetricsTracker>();
            performance.Configure(player, race);
            storage = new MemoryStorage();
            career = root.AddComponent<CareerGameSession>();
            career.Configure(
                player.GetComponent<RaceRoundController>(),
                race,
                performance,
                storage);
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
        public IEnumerator FirstCircuitCompletion_PersistsProgressWalletAndAdvancesToTimeTrial()
        {
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(career.Navigation, Is.Not.Null);
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r01"));
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(race.RequestedActiveRivalCount, Is.EqualTo(3));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));
            Assert.That(career.ActiveChallengeConfiguration.AiDifficulty.PaceMultiplier, Is.EqualTo(.88f).Within(.0001f));
            Assert.That(career.ActiveChallengeConfiguration.AiDifficulty.AggressionMultiplier, Is.EqualTo(.90f).Within(.0001f));

            CompleteCurrentRace();
            yield return null;

            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(career.LastSettlement, Is.Not.Null);
            Assert.That(career.LastSettlement.NodeCompletedNow, Is.True);
            Assert.That(career.Progress.Stars, Is.EqualTo(3));
            Assert.That(career.Progress.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(career.Profile.Coins, Is.EqualTo(250));
            Assert.That(career.Profile.Spirit, Is.EqualTo(5));
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r01"));
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Completed));
            Assert.That(career.Navigation.Nodes[1].State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(storage.Payload, Is.Not.Null.And.Not.Empty);

            var persisted = new CareerPlayerProfileCodec().Decode(storage.Payload);
            Assert.That(persisted.Career.Stars, Is.EqualTo(3));
            Assert.That(persisted.Career.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(persisted.Coins, Is.EqualTo(250));
            Assert.That(persisted.Spirit, Is.EqualTo(5));

            Assert.That(career.TryAdvanceToNextEvent(), Is.True);
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r02"));
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r02"));
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(race.RequestedActiveRivalCount, Is.Zero);
            Assert.That(race.ActiveRivalCount, Is.Zero);
            Assert.That(career.ActiveChallengeConfiguration.EliminationEnabled, Is.False);
            Assert.That(career.LastSettlement, Is.Null);
        }

        [UnityTest]
        public IEnumerator NavigationSelection_MovesAndSelectsWithoutChangingActiveRace()
        {
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));

            var moved = career.MoveCareerSelection(1);
            Assert.That(moved.SelectedNodeId, Is.EqualTo("c01_r02"));
            Assert.That(moved.SelectedNode.State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));

            var selected = career.SelectCareerNode("c01_boss");
            Assert.That(selected.SelectedNodeId, Is.EqualTo("c01_boss"));
            Assert.That(selected.SelectedNode.State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => career.SelectCareerNode("missing"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeededProfile_ResumesAtFirstPlayableIncompleteEvent()
        {
            var seededCareer = new CareerProgress(
                CareerProgress.CurrentVersion,
                3,
                new[] { "c01_r01" },
                new[] { "career:c01_r01:reward:00" });
            var seededProfile = new CareerPlayerProfile(
                seededCareer,
                250,
                5,
                Array.Empty<string>());
            storage.Payload = new CareerPlayerProfileCodec().Encode(seededProfile);

            Object.Destroy(career);
            yield return null;
            career = root.AddComponent<CareerGameSession>();
            career.Configure(
                player.GetComponent<RaceRoundController>(),
                race,
                performance,
                storage);

            Assert.That(career.Progress.Stars, Is.EqualTo(3));
            Assert.That(career.Progress.IsNodeCompleted("c01_r01"), Is.True);
            Assert.That(career.Profile.Coins, Is.EqualTo(250));
            Assert.That(career.Profile.Spirit, Is.EqualTo(5));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r02"));
            Assert.That(career.Navigation.SelectedNodeId, Is.EqualTo("c01_r02"));
            Assert.That(career.Navigation.Nodes[0].State, Is.EqualTo(CareerNodeState.Completed));
            Assert.That(career.Navigation.SelectedNode.State, Is.EqualTo(CareerNodeState.Available));
            Assert.That(race.RequestedActiveRivalCount, Is.Zero);
            Assert.That(race.ActiveRivalCount, Is.Zero);
            Assert.That(career.RecoveredInvalidSave, Is.False);
            Assert.That(new CareerPlayerProfileCodec().Decode(storage.Payload).Career.Stars, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Elimination_PlayerLastAtGate_EndsAsLossAndRestartRestoresRoster()
        {
            SeedEliminationProfile();
            Object.Destroy(career);
            yield return null;
            career = root.AddComponent<CareerGameSession>();
            career.Configure(player.GetComponent<RaceRoundController>(), race, performance, storage);

            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r03"));
            Assert.That(career.ActiveChallengeConfiguration.EliminationEnabled, Is.True);
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));

            race.StartRace();
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(round.AdvanceCountdown(3f), Is.True);

            SetBodyPosition(player, new Vector3(0f, 0f, 0f));
            SetBodyPosition(rivals[0], new Vector3(19f, 0f, 0f));
            SetBodyPosition(rivals[1], new Vector3(14f, 0f, 0f));
            SetBodyPosition(rivals[2], new Vector3(12f, 0f, 0f));
            var leaderCheckpoints = rivals[0].GetComponent<RacerCheckpointTracker>();
            Assert.That(leaderCheckpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            yield return null;

            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(race.WasPlayerEliminated, Is.True);
            Assert.That(race.Position, Is.EqualTo(4));
            Assert.That(race.EliminatedRacerCount, Is.EqualTo(1));
            Assert.That(race.HasPlayerFinishRewardSnapshot, Is.False);
            Assert.That(career.LastSettlement, Is.Not.Null);
            Assert.That(career.LastSettlement.NodeCompletedNow, Is.False);
            Assert.That(career.Progress.IsNodeCompleted("c01_r03"), Is.False);
            Assert.That(career.TryAdvanceToNextEvent(), Is.False);

            Assert.That(career.RestartCurrentEvent(), Is.True);
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(race.WasPlayerEliminated, Is.False);
            Assert.That(race.EliminatedRacerCount, Is.Zero);
            Assert.That(race.ActiveRivalCount, Is.EqualTo(3));
            Assert.That(career.LastSettlement, Is.Null);
        }

        [UnityTest]
        public IEnumerator Elimination_PlayerSurvivesAllGates_FinishesFirstAndCompletesNode()
        {
            SeedEliminationProfile();
            Object.Destroy(career);
            yield return null;
            career = root.AddComponent<CareerGameSession>();
            career.Configure(player.GetComponent<RaceRoundController>(), race, performance, storage);

            race.StartRace();
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(round.AdvanceCountdown(3f), Is.True);

            var playerCheckpoints = player.GetComponent<RacerCheckpointTracker>();
            var rivalOneCheckpoints = rivals[0].GetComponent<RacerCheckpointTracker>();

            Assert.That(rivalOneCheckpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(2));
            Assert.That(race.EliminatedRacerCount, Is.EqualTo(1));

            Assert.That(playerCheckpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(playerCheckpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(race.ActiveRivalCount, Is.EqualTo(1));
            Assert.That(race.EliminatedRacerCount, Is.EqualTo(2));

            Assert.That(playerCheckpoints.TryPassCheckpoint(3), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(race.ActiveRivalCount, Is.Zero);
            Assert.That(race.EliminatedRacerCount, Is.EqualTo(3));
            Assert.That(race.WasPlayerEliminated, Is.False);

            Assert.That(playerCheckpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));
            yield return null;

            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(race.Position, Is.EqualTo(1));
            Assert.That(race.HasPlayerFinishRewardSnapshot, Is.True);
            Assert.That(career.LastSettlement, Is.Not.Null);
            Assert.That(career.LastSettlement.NodeCompletedNow, Is.True);
            Assert.That(career.Progress.IsNodeCompleted("c01_r03"), Is.True);
        }

        private void SeedEliminationProfile()
        {
            var seededCareer = new CareerProgress(
                CareerProgress.CurrentVersion,
                6,
                new[] { "c01_r01", "c01_r02" },
                new[] { "career:c01_r01:reward:00", "career:c01_r02:reward:00" });
            var seededProfile = new CareerPlayerProfile(
                seededCareer,
                600,
                11,
                Array.Empty<string>());
            storage.Payload = new CareerPlayerProfileCodec().Encode(seededProfile);
        }

        private static void SetBodyPosition(ArcadeCarController car, Vector3 position)
        {
            var body = car.GetComponent<Rigidbody>();
            body.position = position;
            car.transform.position = position;
        }

        private void RegisterTestRivals(int count)
        {
            for (var index = 0; index < count; index++)
            {
                var rivalObject = new GameObject($"CAREER SESSION TEST RIVAL {index + 1}");
                rivalObject.transform.SetParent(root.transform, false);
                rivalObject.AddComponent<Rigidbody>();
                var rival = rivalObject.AddComponent<ArcadeCarController>();
                rivalObject.AddComponent<AiRacer>();
                rivals.Add(rival);
                race.RegisterRival(rival);
            }
        }

        private void CompleteCurrentRace()
        {
            race.StartRace();
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(round.AdvanceCountdown(3f), Is.True);
            CompleteCheckpointsOnly();
        }

        private void CompleteCheckpointsOnly()
        {
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
                new Vector3(0f, 0f, 0f),
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = new GameObject($"CareerWaypoint-{index}");
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