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
        public void GyroConversion_UsesUnityHandednessMapping()
        {
            var converted = MobileDriveInputPolicy.GyroToUnity(
                new Quaternion(.1f, .2f, .3f, .4f));

            Assert.That(converted.x, Is.EqualTo(.1f).Within(.0001f));
            Assert.That(converted.y, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(converted.z, Is.EqualTo(-.3f).Within(.0001f));
            Assert.That(converted.w, Is.EqualTo(-.4f).Within(.0001f));
        }

        [Test]
        public void SteeringWheel_PureForwardBackwardPitchCannotSteer()
        {
            var baseline = Quaternion.identity;
            var forwardPitch = Quaternion.AngleAxis(22f, Vector3.right);
            var backwardPitch = Quaternion.AngleAxis(-22f, Vector3.right);

            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelDeltaDegrees(
                    baseline,
                    forwardPitch),
                Is.EqualTo(0f).Within(.0001f));

            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelDeltaDegrees(
                    baseline,
                    backwardPitch),
                Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void SteeringWheel_PureYawCannotSteer()
        {
            var baseline = Quaternion.identity;
            var yaw = Quaternion.AngleAxis(25f, Vector3.up);

            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelDeltaDegrees(
                    baseline,
                    yaw),
                Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void SteeringWheel_ClockwiseIsRightAndCounterClockwiseIsLeft()
        {
            var baseline = Quaternion.identity;
            var clockwise = Quaternion.AngleAxis(-15f, Vector3.forward);
            var counterClockwise = Quaternion.AngleAxis(15f, Vector3.forward);

            var rightDegrees =
                MobileDriveInputPolicy.ResolveSteeringWheelDeltaDegrees(
                    baseline,
                    clockwise);
            var leftDegrees =
                MobileDriveInputPolicy.ResolveSteeringWheelDeltaDegrees(
                    baseline,
                    counterClockwise);

            Assert.That(rightDegrees, Is.EqualTo(15f).Within(.001f));
            Assert.That(leftDegrees, Is.EqualTo(-15f).Within(.001f));
            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelInput(rightDegrees),
                Is.GreaterThan(0f));
            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelInput(leftDegrees),
                Is.LessThan(0f));
        }

        [Test]
        public void SteeringWheelInput_HasDeadZoneAndSameSafeCapAsTouch()
        {
            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelInput(1f),
                Is.EqualTo(0f).Within(.0001f));

            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelInput(90f),
                Is.EqualTo(MobileDriveInputPolicy.TouchSteerMagnitude).Within(.0001f));

            Assert.That(
                MobileDriveInputPolicy.ResolveSteeringWheelInput(-90f),
                Is.EqualTo(-MobileDriveInputPolicy.TouchSteerMagnitude).Within(.0001f));
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
