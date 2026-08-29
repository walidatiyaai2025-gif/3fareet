using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Race
{
    public sealed class RacerCheckpointTracker : MonoBehaviour
    {
        private OrderedCheckpointValidator validator;

        public bool IsConfigured => validator != null;
        public int ExpectedCheckpointIndex => validator?.ExpectedCheckpointIndex ?? -1;
        public int AcceptedCount => validator?.AcceptedCount ?? 0;

        public event Action<int> CheckpointAccepted;
        public event Action<int, CheckpointValidationResult> CheckpointRejected;

        public void Configure(int checkpointCount, int firstExpectedCheckpointIndex = 0)
        {
            validator = new OrderedCheckpointValidator(checkpointCount, firstExpectedCheckpointIndex);
            GetComponent<LastCheckpointTracker>()?.ResetValidatedRaceProgress(firstExpectedCheckpointIndex);
        }

        public CheckpointValidationResult TryPassCheckpoint(int checkpointIndex)
        {
            if (validator == null)
                throw new InvalidOperationException("Checkpoint tracker must be configured before use.");

            var result = validator.TryAccept(checkpointIndex);
            if (result == CheckpointValidationResult.Accepted)
                CheckpointAccepted?.Invoke(checkpointIndex);
            else
                CheckpointRejected?.Invoke(checkpointIndex, result);

            return result;
        }

        public void ResetProgress(int firstExpectedCheckpointIndex = 0)
        {
            if (validator == null)
                throw new InvalidOperationException("Checkpoint tracker must be configured before use.");

            validator.Reset(firstExpectedCheckpointIndex);
            GetComponent<LastCheckpointTracker>()?.ResetValidatedRaceProgress(firstExpectedCheckpointIndex);
        }
    }

    public sealed class RaceCheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex = -1;

        public int CheckpointIndex => checkpointIndex;

        public void Configure(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            checkpointIndex = index;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (checkpointIndex < 0) return;

            var tracker = other.GetComponentInParent<RacerCheckpointTracker>();
            if (tracker == null || !tracker.IsConfigured) return;

            var result = tracker.TryPassCheckpoint(checkpointIndex);
            if (result != CheckpointValidationResult.Accepted) return;

            var recovery = other.GetComponentInParent<LastCheckpointTracker>();
            if (recovery != null)
                recovery.AcceptValidatedRaceCheckpoint(checkpointIndex, transform);
        }
    }

    public static class RaceCheckpointRuntimeBuilder
    {
        public static IReadOnlyList<RaceCheckpointTrigger> Build(
            TrackRuntime track,
            Transform parent,
            float triggerWidth = 12f,
            float triggerHeight = 3f,
            float triggerDepth = 3f)
        {
            if (track == null) throw new ArgumentNullException(nameof(track));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (track.Waypoints.Count < 2)
                throw new ArgumentException("Track must expose at least two ordered waypoints.", nameof(track));
            if (triggerWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(triggerWidth));
            if (triggerHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(triggerHeight));
            if (triggerDepth <= 0f) throw new ArgumentOutOfRangeException(nameof(triggerDepth));

            var checkpoints = new List<RaceCheckpointTrigger>(track.Waypoints.Count);
            for (var i = 0; i < track.Waypoints.Count; i++)
            {
                var waypoint = track.Waypoints[i];
                if (waypoint == null)
                    throw new ArgumentException($"Track waypoint {i} is null.", nameof(track));

                var checkpointObject = new GameObject($"Race Checkpoint {i:00}");
                checkpointObject.transform.SetParent(parent, false);
                checkpointObject.transform.SetPositionAndRotation(waypoint.position, waypoint.rotation);

                var collider = checkpointObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(triggerWidth, triggerHeight, triggerDepth);

                var trigger = checkpointObject.AddComponent<RaceCheckpointTrigger>();
                trigger.Configure(i);
                checkpoints.Add(trigger);
            }

            return checkpoints;
        }

        public static RacerCheckpointTracker EnsureTracker(
            GameObject racer,
            int checkpointCount,
            int firstExpectedCheckpointIndex = 0)
        {
            if (racer == null) throw new ArgumentNullException(nameof(racer));

            var tracker = racer.GetComponent<RacerCheckpointTracker>();
            if (tracker == null) tracker = racer.AddComponent<RacerCheckpointTracker>();
            tracker.Configure(checkpointCount, firstExpectedCheckpointIndex);
            return tracker;
        }
    }
}
