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

        [TestCase(-0.06f)]
        [TestCase(-0.03f)]
        [TestCase(0f)]
        [TestCase(0.03f)]
        [TestCase(0.06f)]
        public void ResolveTiltCruiseThrottle_NeutralBandUsesHandsFreeCruise(float tilt)
        {
            Assert.That(
                MobileDriveInputPolicy.ResolveTiltCruiseThrottle(tilt),
                Is.EqualTo(MobileDriveInputPolicy.TiltCruiseThrottle).Within(.0001f));
        }

        [Test]
        public void ResolveTiltCruiseThrottle_ForwardPitchBoostsProgressivelyAndClamps()
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
        public void ResolveTiltCruiseThrottle_BackwardPitchCoastsWithoutReverseOrBrakeDemand()
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
            Assert.That(
                throttle,
                Is.GreaterThan(0f).And.LessThan(MobileDriveInputPolicy.TiltCruiseThrottle));
        }

        [Test]
        public void TiltSmoothing_ZeroDeltaTimePreservesCurrentInput()
        {
            Assert.That(MobileDriveInputPolicy.SmoothTiltSteer(.2f, -.6f, 0f), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(MobileDriveInputPolicy.SmoothTiltThrottle(.4f, 1f, 0f), Is.EqualTo(.4f).Within(.0001f));
        }
    }
}
