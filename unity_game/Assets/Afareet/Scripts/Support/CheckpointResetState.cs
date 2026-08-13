using System;

namespace Afareet.Support
{
    public struct ResetPose
    {
        public float X, Y, Z, YawDegrees;
        public ResetPose(float x, float y, float z, float yaw) { X = x; Y = y; Z = z; YawDegrees = yaw; }
    }

    public enum ResetReason { None, NoCheckpoint, NotNeeded, Cooldown, Approved }

    public struct ResetDecision
    {
        public bool Allowed;
        public ResetReason Reason;
        public ResetPose Pose;
        public float CooldownRemaining;
    }

    public sealed class CheckpointResetState
    {
        private bool hasCheckpoint;
        private ResetPose pose;
        private double lastResetAt = -1000d;

        public void RecordValidCheckpoint(ResetPose value) { pose = value; hasCheckpoint = true; }

        public ResetDecision Request(double now, bool upsideDown, bool stuck, float speedMps)
        {
            if (!hasCheckpoint) return Denied(ResetReason.NoCheckpoint, 0f);
            if (!upsideDown && !stuck && speedMps > 1.5f) return Denied(ResetReason.NotNeeded, 0f);
            var remaining = (float)Math.Max(0d, 3d - (now - lastResetAt));
            if (remaining > 0f) return Denied(ResetReason.Cooldown, remaining);
            lastResetAt = now;
            return new ResetDecision { Allowed = true, Reason = ResetReason.Approved, Pose = pose };
        }

        private ResetDecision Denied(ResetReason reason, float remaining)
        {
            return new ResetDecision { Allowed = false, Reason = reason, Pose = pose, CooldownRemaining = remaining };
        }
    }
}
