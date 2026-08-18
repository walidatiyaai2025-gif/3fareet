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
    public sealed class CareerRewardMultiplierPlayModeTests
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
        private CareerGameSession career;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("CAREER REWARD MULTIPLIER TEST ROOT");
            playerObject = new GameObject("CAREER REWARD MULTIPLIER TEST PLAYER");
            playerObject.AddComponent<Rigidbody>();
            player = playerObject.AddComponent<ArcadeCarController>();

            race = root.AddComponent<RaceDirector>();
            race.Configure(player, BuildTrack());
            var performance = root.AddComponent<RacePerformanceMetricsTracker>();
            performance.Configure(player, race);
            career = root.AddComponent<CareerGameSession>();
            career.Configure(
                player.GetComponent<RaceRoundController>(),
                race,
                performance,
                new MemoryStorage());
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
        public IEnumerator EnchantedPound_DoublesFirstClaimedCoinsAndReplayGrantsNothing()
        {
            Assert.That(career.ActiveDefinition.Node.Id, Is.EqualTo("c01_r01"));
            Assert.That(career.Profile.Coins, Is.Zero);

            BeginRace();
            var use = race.TryUsePlayerPowerUp(PowerUpKind.EnchantedPound);
            Assert.That(use, Is.Not.Null);
            Assert.That(use.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            CompleteLap();
            yield return null;

            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Results));
            Assert.That(career.LastSettlement, Is.Not.Null);
            Assert.That(career.LastSettlement.CoinsGranted, Is.EqualTo(250));
            Assert.That(career.LastCoinRewardSettlement, Is.Not.Null);
            Assert.That(career.LastCoinRewardSettlement.BaseRewardUnits, Is.EqualTo(250));
            Assert.That(career.LastCoinRewardSettlement.RewardMultiplier, Is.EqualTo(2d).Within(.000001d));
            Assert.That(career.LastCoinRewardSettlement.SettledRewardUnits, Is.EqualTo(500));
            Assert.That(career.Profile.Coins, Is.EqualTo(500));

            Assert.That(career.RestartCurrentEvent(), Is.True);
            Assert.That(career.LastSettlement, Is.Null);
            Assert.That(career.LastCoinRewardSettlement, Is.Null);
            BeginCountdownAlreadyStarted();
            var replayUse = race.TryUsePlayerPowerUp(PowerUpKind.EnchantedPound);
            Assert.That(replayUse, Is.Not.Null);
            Assert.That(replayUse.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));
            CompleteLap();
            yield return null;

            Assert.That(career.LastSettlement, Is.Not.Null);
            Assert.That(career.LastSettlement.CoinsGranted, Is.Zero);
            Assert.That(career.LastSettlement.GrantedAnyReward, Is.False);
            Assert.That(career.LastCoinRewardSettlement, Is.Null);
            Assert.That(career.Profile.Coins, Is.EqualTo(500));
        }

        private void BeginRace()
        {
            race.StartRace();
            BeginCountdownAlreadyStarted();
        }

        private void BeginCountdownAlreadyStarted()
        {
            var round = player.GetComponent<RaceRoundController>();
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Countdown));
            Assert.That(round.AdvanceCountdown(3f), Is.True);
            Assert.That(race.Phase, Is.EqualTo(RaceRoundPhase.Racing));
        }

        private void CompleteLap()
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
                Vector3.zero,
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = new GameObject($"RewardWaypoint-{index}");
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
