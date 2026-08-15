using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class VehicleRecoveryPolicyTests
    {
        [Test]
        public void SteeringSpeedFactor_RetainsUsefulAuthorityAtLowSpeed()
        {
            Assert.That(VehicleHandlingPolicy.SteeringSpeedFactor(0f),
                Is.EqualTo(VehicleHandlingPolicy.LowSpeedSteerFactor).Within(0.0001f));
            Assert.That(VehicleHandlingPolicy.LowSpeedSteerFactor, Is.GreaterThanOrEqualTo(0.65f));
            Assert.That(VehicleHandlingPolicy.SteeringSpeedFactor(7f),
                Is.GreaterThan(VehicleHandlingPolicy.SteeringSpeedFactor(0f)));
            Assert.That(VehicleHandlingPolicy.SteeringSpeedFactor(100f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void StuckTimer_OnlyAccumulatesForGroundedStationaryDriveIntent()
        {
            var timer = VehicleRecoveryPolicy.AdvanceStuckTimer(0f, true, 1f, 1f, false, 1f);
            Assert.That(timer, Is.EqualTo(1f).Within(0.0001f));

            Assert.That(VehicleRecoveryPolicy.AdvanceStuckTimer(timer, false, 1f, 1f, false, 1f), Is.EqualTo(0f));
            Assert.That(VehicleRecoveryPolicy.AdvanceStuckTimer(timer, true, 20f, 1f, false, 1f), Is.EqualTo(0f));
            Assert.That(VehicleRecoveryPolicy.AdvanceStuckTimer(timer, true, 1f, 0.2f, false, 1f), Is.EqualTo(0f));
            Assert.That(VehicleRecoveryPolicy.AdvanceStuckTimer(timer, true, 1f, 1f, true, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void StuckTimer_TriggersOnlyAfterSustainedIntent()
        {
            var timer = VehicleRecoveryPolicy.StuckSecondsBeforeAutoRecovery - 0.01f;
            Assert.That(VehicleRecoveryPolicy.ShouldAutoRecover(timer), Is.False);

            timer = VehicleRecoveryPolicy.AdvanceStuckTimer(timer, true, 0.5f, -1f, false, 0.02f);
            Assert.That(VehicleRecoveryPolicy.ShouldAutoRecover(timer), Is.True);
        }

        [Test]
        public void SafeRecoveryPosition_AddsCenterlineClearanceInTrackFrame()
        {
            var center = new Vector3(10f, 0f, 20f);
            var rotation = Quaternion.Euler(0f, 90f, 0f);
            var position = VehicleRecoveryPolicy.SafeRecoveryPosition(center, rotation);

            Assert.That(position.y, Is.EqualTo(VehicleRecoveryPolicy.RecoveryUpOffsetMeters).Within(0.001f));
            Assert.That(Vector3.Distance(
                    new Vector3(position.x, 0f, position.z),
                    new Vector3(center.x, 0f, center.z)),
                Is.EqualTo(VehicleRecoveryPolicy.RecoveryForwardOffsetMeters).Within(0.001f));
            Assert.That(Vector3.Dot((position - center).normalized, rotation * Vector3.forward), Is.GreaterThan(0.85f));
        }
    }
}
