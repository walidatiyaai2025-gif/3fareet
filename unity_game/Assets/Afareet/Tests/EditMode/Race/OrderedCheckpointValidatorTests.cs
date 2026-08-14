using Afareet.Race;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class OrderedCheckpointValidatorTests
    {
        [Test]
        public void RejectsSkippedCheckpointWithoutAdvancing()
        {
            var validator = new OrderedCheckpointValidator(4);

            var result = validator.TryAccept(2);

            Assert.That(result, Is.EqualTo(CheckpointValidationResult.OutOfOrder));
            Assert.That(validator.ExpectedCheckpointIndex, Is.EqualTo(0));
            Assert.That(validator.AcceptedCount, Is.EqualTo(0));
        }

        [Test]
        public void RejectsDuplicateCheckpointWithoutDoubleCounting()
        {
            var validator = new OrderedCheckpointValidator(4);
            Assert.That(validator.TryAccept(0), Is.EqualTo(CheckpointValidationResult.Accepted));

            var result = validator.TryAccept(0);

            Assert.That(result, Is.EqualTo(CheckpointValidationResult.Duplicate));
            Assert.That(validator.ExpectedCheckpointIndex, Is.EqualTo(1));
            Assert.That(validator.AcceptedCount, Is.EqualTo(1));
        }

        [Test]
        public void AcceptsOnlyOrderedSequenceAndWrapsExpectedIndex()
        {
            var validator = new OrderedCheckpointValidator(3);

            Assert.That(validator.TryAccept(0), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(validator.TryAccept(1), Is.EqualTo(CheckpointValidationResult.Accepted));
            Assert.That(validator.TryAccept(2), Is.EqualTo(CheckpointValidationResult.Accepted));

            Assert.That(validator.ExpectedCheckpointIndex, Is.EqualTo(0));
            Assert.That(validator.AcceptedCount, Is.EqualTo(3));
        }

        [Test]
        public void TrackerRaisesAcceptedEventOnlyForValidCheckpoint()
        {
            var racer = new GameObject("Checkpoint Test Racer");
            try
            {
                var tracker = racer.AddComponent<RacerCheckpointTracker>();
                tracker.Configure(3);
                var acceptedEvents = 0;
                var rejectedEvents = 0;
                tracker.CheckpointAccepted += _ => acceptedEvents++;
                tracker.CheckpointRejected += (_, __) => rejectedEvents++;

                Assert.That(tracker.TryPassCheckpoint(1), Is.EqualTo(CheckpointValidationResult.OutOfOrder));
                Assert.That(tracker.TryPassCheckpoint(0), Is.EqualTo(CheckpointValidationResult.Accepted));

                Assert.That(acceptedEvents, Is.EqualTo(1));
                Assert.That(rejectedEvents, Is.EqualTo(1));
                Assert.That(tracker.ExpectedCheckpointIndex, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(racer);
            }
        }

        [Test]
        public void RuntimeBuilderCreatesOrderedTriggerVolumesFromWaypoints()
        {
            var root = new GameObject("Checkpoint Builder Test");
            try
            {
                var track = new TrackRuntime();
                for (var i = 0; i < 3; i++)
                {
                    var waypoint = new GameObject($"Waypoint {i}").transform;
                    waypoint.SetParent(root.transform);
                    waypoint.SetPositionAndRotation(new Vector3(i * 10f, 0f, i * 2f), Quaternion.Euler(0f, i * 15f, 0f));
                    track.Waypoints.Add(waypoint);
                }

                var triggers = RaceCheckpointRuntimeBuilder.Build(track, root.transform, 10f, 4f, 2f);

                Assert.That(triggers.Count, Is.EqualTo(3));
                for (var i = 0; i < triggers.Count; i++)
                {
                    Assert.That(triggers[i].CheckpointIndex, Is.EqualTo(i));
                    var collider = triggers[i].GetComponent<BoxCollider>();
                    Assert.That(collider, Is.Not.Null);
                    Assert.That(collider.isTrigger, Is.True);
                    Assert.That(collider.size, Is.EqualTo(new Vector3(10f, 4f, 2f)));
                    Assert.That(triggers[i].transform.position, Is.EqualTo(track.Waypoints[i].position));
                    Assert.That(triggers[i].transform.rotation, Is.EqualTo(track.Waypoints[i].rotation));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
