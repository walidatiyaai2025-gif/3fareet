using System.Collections;
using Afareet.Race;
using Afareet.Vehicle;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Afareet.Tests.PlayMode
{
    public sealed class PlayerPowerUpPlayModeTests
    {
        private GameObject directorObject;
        private GameObject playerObject;
        private GameObject rivalObject;
        private RaceDirector director;
        private ArcadeCarController player;
        private ArcadeCarController rival;
        private TrackRuntime track;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            directorObject = new GameObject("RaceDirector-PlayerPowerUpTest");
            director = directorObject.AddComponent<RaceDirector>();
            playerObject = CreateCar("Player-PowerUpTest", out player);
            rivalObject = CreateCar("Rival-PowerUpTest", out rival);
            track = BuildTrack();

            var rivalAi = rivalObject.AddComponent<AiRacer>();
            rivalAi.Configure(track.Waypoints, 1);
            director.Configure(player, track);
            director.RegisterRival(rival);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (directorObject != null) Object.Destroy(directorObject);
            if (playerObject != null) Object.Destroy(playerObject);
            if (rivalObject != null) Object.Destroy(rivalObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerPowerUp_IsUnavailableBeforeRaceAndWhilePaused()
        {
            Assert.That(director.GetPlayerPowerUpInventory(), Is.Empty);
            Assert.That(director.TryUsePlayerPowerUp(PowerUpKind.NitroSpirit), Is.Null);

            StartRacing();
            Assert.That(director.GetPlayerPowerUpInventory().Count, Is.EqualTo(5));
            Assert.That(director.SetPaused(true), Is.True);
            Assert.That(director.TryUsePlayerPowerUp(PowerUpKind.NitroSpirit), Is.Null);
            Assert.That(director.SetPaused(false), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerPowerUp_SelfUseConsumesChargeAndBindsPlayerTarget()
        {
            StartRacing();

            var result = director.TryUsePlayerPowerUp(PowerUpKind.NitroSpirit);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(result.SourceRacerId, Is.EqualTo("PLAYER"));
            Assert.That(result.TargetRacerId, Is.EqualTo("PLAYER"));
            Assert.That(result.RemainingCharges, Is.EqualTo(1));
            Assert.That(Inventory(PowerUpKind.NitroSpirit).Charges, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerPowerUp_WorldDeployableUsesNoRacerTarget()
        {
            StartRacing();
            var playerCheckpoints = player.GetComponent<RacerCheckpointTracker>();
            Assert.That(
                playerCheckpoints.TryPassCheckpoint(1),
                Is.EqualTo(CheckpointValidationResult.Accepted));

            var shard = director.TryUsePlayerPowerUp(PowerUpKind.AsphaltShard);
            Assert.That(shard, Is.Not.Null);
            Assert.That(shard.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(shard.TargetRacerId, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RestartRace_ResetsPlayerPowerUpInventory()
        {
            StartRacing();
            Assert.That(
                director.TryUsePlayerPowerUp(PowerUpKind.NitroSpirit).Status,
                Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            Assert.That(Inventory(PowerUpKind.NitroSpirit).Charges, Is.EqualTo(1));

            var checkpoints = player.GetComponent<RacerCheckpointTracker>();
            Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(3), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));
            yield return null;

            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(director.RestartRace(), Is.True);
            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(Inventory(PowerUpKind.NitroSpirit).Charges, Is.EqualTo(2));
            yield return null;
        }

        private void StartRacing()
        {
            director.StartRace();
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(round.AdvanceCountdown(3f), Is.True);
            Assert.That(director.Phase, Is.EqualTo(RaceRoundPhase.Racing));
        }

        private PowerUpInventorySnapshot Inventory(PowerUpKind kind)
        {
            var inventory = director.GetPlayerPowerUpInventory();
            for (var index = 0; index < inventory.Count; index++)
                if (inventory[index].Kind == kind)
                    return inventory[index];
            Assert.Fail($"Missing player inventory slot for {kind}.");
            return null;
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
                var waypoint = new GameObject($"Waypoint-PlayerPowerUpTest-{index}");
                waypoint.transform.SetParent(directorObject.transform, false);
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

        private static GameObject CreateCar(string name, out ArcadeCarController controller)
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<Rigidbody>();
            controller = gameObject.AddComponent<ArcadeCarController>();
            return gameObject;
        }
    }
}
