using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class LastCheckpointTracker : MonoBehaviour
    {
        private Transform lastCheckpoint;
        private float nextSample;

        public bool HasCheckpoint => lastCheckpoint != null;
        public Vector3 Position => lastCheckpoint == null ? transform.position : lastCheckpoint.position;
        public Quaternion Rotation => lastCheckpoint == null ? transform.rotation : lastCheckpoint.rotation;

        private void Update()
        {
            if (Time.time < nextSample) return;
            nextSample = Time.time + .25f;
            if (Vector3.Dot(transform.up, Vector3.up) < .55f) return;

            Transform nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < 128; i++)
            {
                var waypoint = GameObject.Find($"Waypoint {i:00}");
                if (waypoint == null)
                {
                    if (i > 0) break;
                    continue;
                }

                var distance = (waypoint.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = waypoint.transform;
                nearestDistance = distance;
            }

            if (nearest != null && nearestDistance <= 324f)
                lastCheckpoint = nearest;
        }
    }
}
