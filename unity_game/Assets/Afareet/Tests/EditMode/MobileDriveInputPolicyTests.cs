using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class MobileDriveInputPolicyTests
    {
        [Test]
        public void TouchSteering_IsReducedFromFullLock()
        {
            Assert.That(MobileDriveInputPolicy.ResolveTouchSteer(-1f), Is.EqualTo(-0.60f).Within(0.0001f));
            Assert.That(MobileDriveInputPolicy.ResolveTouchSteer(1f), Is.EqualTo(0.60f).Within(0.0001f));
            Assert.That(Mathf.Abs(MobileDriveInputPolicy.ResolveTouchSteer(1f)), Is.LessThan(1f));
        }

        [Test]
        public void TiltSteering_HasDeadZoneAndSameSafeCap()
        {
            Assert.That(MobileDriveInputPolicy.ResolveTiltSteer(0.04f), Is.EqualTo(0f));
            Assert.That(MobileDriveInputPolicy.ResolveTiltSteer(10f), Is.EqualTo(MobileDriveInputPolicy.TouchSteerMagnitude));
            Assert.That(MobileDriveInputPolicy.ResolveTiltSteer(-10f), Is.EqualTo(-MobileDriveInputPolicy.TouchSteerMagnitude));
        }

        [Test]
        public void BrakeReverse_BrakesForwardMotionThenSelectsReverseNearStop()
        {
            MobileDriveInputPolicy.ResolveBrakeReverse(25f, out var movingThrottle, out var movingBrake);
            Assert.That(movingThrottle, Is.EqualTo(0f));
            Assert.That(movingBrake, Is.True);

            MobileDriveInputPolicy.ResolveBrakeReverse(0f, out var stoppedThrottle, out var stoppedBrake);
            Assert.That(stoppedThrottle, Is.EqualTo(MobileDriveInputPolicy.ReverseThrottle));
            Assert.That(stoppedBrake, Is.False);
        }

        [Test]
        public void ResetToSpawn_ClearsLatchedPlayerInputs()
        {
            var carObject = new GameObject("mobile-recovery-test");
            try
            {
                carObject.AddComponent<Rigidbody>();
                var controller = carObject.AddComponent<ArcadeCarController>();
                controller.SetPlayerInput(1f, 1f, true, true, true);

                controller.ResetToSpawn();

                Assert.That(controller.CurrentThrottleInput, Is.EqualTo(0f));
                Assert.That(controller.CurrentSteerInput, Is.EqualTo(0f));
                Assert.That(controller.CurrentBrakeInput, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(carObject);
            }
        }
    }
}
