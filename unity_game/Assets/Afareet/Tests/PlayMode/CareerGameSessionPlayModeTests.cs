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
            root = new GameObject("CAREER SESSION TEST ROOT");
            playerObject = new GameObject("CAREER SESSION TEST PLAYER");
            playerObject.AddComponent<Rigidbody>();
            player = playerObject.AddComponent<ArcadeCarController>();

            race = root.AddComponent<RaceDirector>();
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
            Assert.That(career.LastSettlement, Is.Null);
        }

        [UnityTest]
        public IEnumerator NavigationSelection_MovesAndSelectsWithoutChangingActiveRace()
        {
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));

            var moved = career.MoveCareerSelection(1);
            Assert.That(moved.SelectedNodeId, Is.EqualTo("c01_r02"));
            Assert.That(moved.SelectedNode.State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));

            var selected = career.SelectCareerNode("c01_boss");
            Assert.That(selected.SelectedNodeId, Is.EqualTo("c01_boss"));
            Assert.That(selected.SelectedNode.State, Is.EqualTo(CareerNodeState.Locked));
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
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
            Assert.That(career.RecoveredInvalidSave, Is.False);
            Assert.That(new CareerPlayerProfileCodec().Decode(storage.Payload).Career.Stars, Is.EqualTo(3));
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