using System.Collections.Generic;
using Afareet.Race;
using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class RivalLifecycleTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created) if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        [Test]
        public void MotionGuardTriggersOnlyAfterLowSpeedDelay()
        {
            var guard = new RivalMotionGuard(4f, 2.5f);
            Assert.That(guard.Observe(0f, 1f), Is.False);
            Assert.That(guard.Observe(0f, 1f), Is.False);
            Assert.That(guard.Observe(0f, .5f), Is.True);
            Assert.That(guard.LowSpeedSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void MovingAgainClearsLowSpeedTimer()
        {
            var guard = new RivalMotionGuard(4f, 2f);
            guard.Observe(0f, 1.5f);
            guard.Observe(20f, .1f);
            Assert.That(guard.LowSpeedSeconds, Is.EqualTo(0f));
            Assert.That(guard.Observe(0f, .6f), Is.False);
        }

        [Test]
        public void ResetControllerUsesLastAcceptedCheckpoint()
        {
            var waypoints = Waypoints(4);
            var rival = NewObject("Rival");
            rival.AddComponent<Rigidbody>();
            rival.AddComponent<ArcadeCarController>();
            var checkpoints = rival.AddComponent<RacerCheckpointTracker>();
            checkpoints.Configure(4, 1);
            var reset = rival.AddComponent<RivalResetController>();
            reset.Configure(waypoints, checkpoints, 4f, 1f);
            reset.SetActive(true);

            rival.transform.position = new Vector3(50f, 0f, 50f);
            Assert.That(reset.Evaluate(0f, 1f), Is.True);
            Assert.That(reset.LastResetWaypointIndex, Is.EqualTo(0));
            Assert.That(Vector3.Distance(rival.transform.position, waypoints[0].position + Vector3.up * .75f), Is.LessThan(.001f));

            Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            rival.transform.position = new Vector3(60f, 0f, 60f);
            Assert.That(reset.Evaluate(0f, 1f), Is.True);
            Assert.That(reset.LastResetWaypointIndex, Is.EqualTo(1));
            Assert.That(reset.ResetCount, Is.EqualTo(2));
        }

        [Test]
        public void RivalFinishesOnlyAfterCompleteOrderedLap()
        {
            var rival = NewObject("Rival Finish");
            var checkpoints = rival.AddComponent<RacerCheckpointTracker>();
            var lap = rival.AddComponent<OneLapRaceTracker>();
            lap.Configure(4);
            lap.StartRace();
            lap.AdvanceTime(12.5f);

            Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.OutOfOrder));
            Assert.That(lap.IsFinished, Is.False);
            Assert.That(checkpoints.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(2), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(checkpoints.TryPassCheckpoint(3), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(lap.IsFinished, Is.False);
            Assert.That(checkpoints.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));

            Assert.That(lap.IsFinished, Is.True);
            Assert.That(lap.CompletedLaps, Is.EqualTo(1));
            Assert.That(lap.FinishTime, Is.EqualTo(12.5f).Within(.001f));
        }

        private List<Transform> Waypoints(int count)
        {
            var result = new List<Transform>();
            for (var i = 0; i < count; i++)
            {
                var obj = NewObject($"W{i}");
                obj.transform.position = new Vector3(i * 10f, 0f, 0f);
                result.Add(obj.transform);
            }
            return result;
        }

        private GameObject NewObject(string name)
        {
            var obj = new GameObject(name);
            created.Add(obj);
            return obj;
        }
    }
}
