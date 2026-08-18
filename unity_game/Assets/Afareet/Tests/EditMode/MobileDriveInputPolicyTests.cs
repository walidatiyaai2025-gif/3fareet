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

        [TestCase(-0.06f)]
        [TestCase(-0.03f)]
        [TestCase(0f)]
        [TestCase(0.03f)]
        [TestCase(0.06f)]
        public void TiltCruise_NeutralBandUsesHandsFreeCruise(float tilt)
        {
            Assert.That(
                MobileDriveInputPolicy.ResolveTiltCruiseThrottle(tilt),
                Is.EqualTo(MobileDriveInputPolicy.TiltCruiseThrottle).Within(.0001f));
        }

        [Test]
        public void TiltCruise_ForwardPitchBoostsProgressivelyAndClamps()
        {
            var neutral = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(0f);
            var moderate = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(.30f);
            var strong = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(.55f);
            var extreme = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(2f);

            Assert.That(moderate, Is.GreaterThan(neutral).And.LessThanOrEqualTo(1f));
            Assert.That(strong, Is.GreaterThan(moderate).And.LessThanOrEqualTo(1f));
            Assert.That(extreme, Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void TiltCruise_BackwardPitchCoastsWithoutReverseDemand()
        {
            var neutral = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(0f);
            var moderate = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(-.30f);
            var strong = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(-.55f);
            var extreme = MobileDriveInputPolicy.ResolveTiltCruiseThrottle(-2f);

            Assert.That(moderate, Is.LessThan(neutral).And.GreaterThanOrEqualTo(0f));
            Assert.That(strong, Is.LessThan(moderate).And.GreaterThanOrEqualTo(0f));
            Assert.That(extreme, Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void TiltSmoothing_MovesTowardTargetWithoutOneFrameSnap()
        {
            var steer = MobileDriveInputPolicy.SmoothTiltSteer(0f, .6f, 1f / 60f);
            var throttle = MobileDriveInputPolicy.SmoothTiltThrottle(
                0f,
                MobileDriveInputPolicy.TiltCruiseThrottle,
                1f / 60f);

            Assert.That(steer, Is.GreaterThan(0f).And.LessThan(.6f));
            Assert.That(throttle,
                Is.GreaterThan(0f).And.LessThan(MobileDriveInputPolicy.TiltCruiseThrottle));
        }

        [Test]
        public void TiltSmoothing_ZeroDeltaTimePreservesCurrentInput()
        {
            Assert.That(MobileDriveInputPolicy.SmoothTiltSteer(.2f, -.6f, 0f), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(MobileDriveInputPolicy.SmoothTiltThrottle(.4f, 1f, 0f), Is.EqualTo(.4f).Within(.0001f));
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
        public void ResetToSpawn_ClearsLatchedPlayerInputsAndLocksImmediateReapply()
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
                Assert.That(controller.RecoveryInputLockRemaining,
                    Is.EqualTo(VehicleRecoveryPolicy.PostRecoveryInputLockSeconds).Within(0.0001f));

                controller.SetPlayerInput(1f, 1f, true, true, false);
                Assert.That(controller.CurrentThrottleInput, Is.EqualTo(0f));
                Assert.That(controller.CurrentSteerInput, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(carObject);
            }
        }
    }
}
