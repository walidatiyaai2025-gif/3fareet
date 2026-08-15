using System;
using UnityEngine;

namespace Afareet.Race
{
    public readonly struct CornerSpeedPlan
    {
        public CornerSpeedPlan(float severity, float targetSpeed, float brake)
        {
            Severity01 = severity;
            TargetSpeedKph = targetSpeed;
            Brake01 = brake;
        }
        public float Severity01 { get; }
        public float TargetSpeedKph { get; }
        public float Brake01 { get; }
    }

    public static class CornerSpeedPolicy
    {
        public static float Severity(Vector3 previous, Vector3 current, Vector3 next)
        {
            previous.y = current.y = next.y = 0f;
            var incoming = current - previous;
            var outgoing = next - current;
            if (incoming.sqrMagnitude <= .0001f || outgoing.sqrMagnitude <= .0001f) return 1f;
            return Mathf.Clamp01(Vector3.Angle(incoming.normalized, outgoing.normalized) / 110f);
        }

        public static CornerSpeedPlan Plan(float severity01, float speedKph, float straightKph = 125f, float cornerKph = 50f)
        {
            if (severity01 < 0f || severity01 > 1f) throw new ArgumentOutOfRangeException(nameof(severity01));
            if (speedKph < 0f || straightKph <= 0f || cornerKph <= 0f || cornerKph > straightKph) throw new ArgumentOutOfRangeException();
            var target = Mathf.Lerp(straightKph, cornerKph, severity01);
            var overspeed = Mathf.Max(0f, speedKph - target);
            var brake = Mathf.Clamp01(overspeed / Mathf.Max(15f, target * .45f));
            return new CornerSpeedPlan(severity01, target, brake);
        }
    }
}
