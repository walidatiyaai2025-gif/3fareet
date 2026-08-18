using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests.Vehicle
{
    public sealed class MobileDriveInputPolicyTests
    {
        [TestCase(-0.08f)]
        [TestCase(-0.04f)]
        [TestCase(0f)]
        [TestCase(0.04f)]
        [TestCase(0.08f)]
        public void ResolveTiltSteer_InsideDeadZone_ReturnsZero(float tilt)
        {
            Assert.That(MobileDriveInputPolicy.ResolveTiltSteer(tilt), Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void ResolveTiltSteer_PreservesDirectionAndClampsToMobileAuthority()
        {
            var left = MobileDriveInputPolicy.ResolveTiltSteer(-1f);
            var right = MobileDriveInputPolicy.ResolveTiltSteer(1f);

            Assert.That(left, Is.EqualTo(-MobileDriveInputPolicy.TouchSteerMagnitude).Within(.0001f));
            Assert.That(right, Is.EqualTo(MobileDriveInputPolicy.TouchSteerMagnitude).Within(.0001f));
        }

        [TestCase(-1f)]
        [TestCase(-0.2f)]
        [TestCase(0f)]
        [TestCase(0.03f)]
        [TestCase(0.06f)]
        public void ResolveTiltThrottle_BackwardOrDeadZoneNeverAccelerates(float tilt)
        {
            Assert.That(MobileDriveInputPolicy.ResolveTiltThrottle(tilt), Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void ResolveTiltThrottle_ForwardPitchRampsAndClamps()
        {
            var moderate = MobileDriveInputPolicy.ResolveTiltThrottle(.30f);
            var strong = MobileDriveInputPolicy.ResolveTiltThrottle(.55f);
            var extreme = MobileDriveInputPolicy.ResolveTiltThrottle(2f);

            Assert.That(moderate, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(strong, Is.GreaterThan(moderate).And.LessThanOrEqualTo(1f));
            Assert.That(extreme, Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void TiltSmoothing_MovesTowardTargetWithoutOneFrameSnap()
        {
            var steer = MobileDriveInputPolicy.SmoothTiltSteer(0f, .6f, 1f / 60f);
            var throttle = MobileDriveInputPolicy.SmoothTiltThrottle(0f, 1f, 1f / 60f);

            Assert.That(steer, Is.GreaterThan(0f).And.LessThan(.6f));
            Assert.That(throttle, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void TiltSmoothing_ZeroDeltaTimePreservesCurrentInput()
        {
            Assert.That(MobileDriveInputPolicy.SmoothTiltSteer(.2f, -.6f, 0f), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(MobileDriveInputPolicy.SmoothTiltThrottle(.4f, 1f, 0f), Is.EqualTo(.4f).Within(.0001f));
        }
    }
}
