using System.Collections.Generic;
using Afareet.Race;
using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.PlayMode
{
    public sealed class AiRacerNavigationRecoveryPlayModeTests
    {
        private GameObject root;
        private GameObject rivalObject;
        private AiRacer ai;
        private RacerCheckpointTracker checkpoints;
        private RivalResetController reset;
        private readonly List<Transform> waypoints = new();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("AiRacer-Navigation-Recovery-Test");
            BuildWaypoints();

            rivalObject = new GameObject("Rival-Navigation-Recovery-Test");
            rivalObject.transform.SetParent(root.transform, false);
            rivalObject.AddComponent<Rigidbody>();
            rivalObject.AddComponent<ArcadeCarController>();
            ai = rivalObject.AddComponent<AiRacer>();
            ai.Configure(waypoints, 0);

            checkpoints = rivalObject.AddComponent<RacerCheckpointTracker>();
            checkpoints.Configure(waypoints.Count, 1);

            reset = rivalObject.AddComponent<RivalResetController>();
            reset.Configure(waypoints, checkpoints, lowSpeedKph: 4f, delaySeconds: .1f);

            // AiRacer is created before the race progress components in production too.
            // Cycling it once mirrors the RaceDirector freeze/release lifecycle and binds
            // the recovery/reset callbacks that are available by race start.
            ai.enabled = false;
            ai.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            waypoints.Clear();
        }

        [Test]
        public void ReenableAfterProgressReset_UsesFreshExpectedCheckpoint()
        {
            Assert.That(checkpoints.ExpectedCheckpointIndex, Is.EqualTo(1));

            ai.SynchronizeNavigation(3);
            Assert.That(ai.NavigationWaypointIndex, Is.EqualTo(3));

            ai.enabled = false;
            checkpoints.ResetProgress(1);
            ai.enabled = true;

            Assert.That(ai.NavigationWaypointIndex, Is.EqualTo(1),
                "A restarted rival must not retain the previous race's navigation waypoint.");
        }

        [Test]
        public void RivalRecovery_ResynchronizesAiToNextExpectedCheckpoint()
        {
            Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.ExpectedCheckpointIndex, Is.EqualTo(3));

            ai.SynchronizeNavigation(0);
            Assert.That(ai.NavigationWaypointIndex, Is.Zero);

            reset.SetActive(true);
            Assert.That(reset.Evaluate(0f, .11f), Is.True);

            Assert.That(reset.LastResetWaypointIndex, Is.EqualTo(2));
            Assert.That(ai.NavigationWaypointIndex, Is.EqualTo(3),
                "Recovery must steer toward the checkpoint after the validated reset waypoint.");
        }

        private void BuildWaypoints()
        {
            var points = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(20f, 0f, 0f),
                new Vector3(20f, 0f, 20f),
                new Vector3(0f, 0f, 20f)
            };

            for (var index = 0; index < points.Length; index++)
            {
                var waypoint = new GameObject($"Waypoint-{index}").transform;
                waypoint.SetParent(root.transform, false);
                waypoint.position = points[index];
                waypoints.Add(waypoint);
            }

            for (var index = 0; index < waypoints.Count; index++)
            {
                var current = waypoints[index];
                var next = waypoints[(index + 1) % waypoints.Count];
                current.rotation = Quaternion.LookRotation((next.position - current.position).normalized);
            }
        }
    }
}
