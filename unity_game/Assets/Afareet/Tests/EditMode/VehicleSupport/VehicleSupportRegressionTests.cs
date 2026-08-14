using Afareet.UI;
using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests.VehicleSupport
{
    public sealed class VehicleSupportRegressionTests
    {
        [Test]
        public void SteeringResponse_ScalesDownAtHighVirtualSpeed()
        {
            Assert.That(SteeringResponse.Evaluate(0.8f, 0f, 100f, 0.5f), Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(SteeringResponse.Evaluate(2f, 100f, 100f, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void SpeedGovernor_TapersPositiveThrottleInsideSoftZone()
        {
            Assert.That(ArcadeGameSpeedGovernor.EvaluateThrottle(1f, 70f, 100f, 0.2f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ArcadeGameSpeedGovernor.EvaluateThrottle(1f, 95f, 100f, 0.2f), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(ArcadeGameSpeedGovernor.EvaluateThrottle(-0.5f, 120f, 100f, 0.2f), Is.EqualTo(-0.5f).Within(0.0001f));
        }

        [Test]
        public void TractionModel_ReducesDriveWhenSlipExceedsThreshold()
        {
            Assert.That(ArcadeTractionModel.LimitDrive(1f, 0.5f, 1f, 0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ArcadeTractionModel.LimitDrive(1f, 2f, 1f, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void DriftGrip_BlendsBetweenRoadAndDriftProfiles()
        {
            Assert.That(ArcadeDriftGripModel.Evaluate(10f, 4f, 0f), Is.EqualTo(10f).Within(0.0001f));
            Assert.That(ArcadeDriftGripModel.Evaluate(10f, 4f, 0.5f), Is.EqualTo(7f).Within(0.0001f));
            Assert.That(ArcadeDriftGripModel.Evaluate(10f, 4f, 1f), Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void OffRoadSurface_ReducesGripAccelerationAndTopSpeed()
        {
            var asphalt = ArcadeSurfaceResponse.For(ArcadeSurfaceType.Asphalt);
            var offRoad = ArcadeSurfaceResponse.For(ArcadeSurfaceType.OffRoad);

            Assert.That(offRoad.Grip, Is.LessThan(asphalt.Grip));
            Assert.That(offRoad.Acceleration, Is.LessThan(asphalt.Acceleration));
            Assert.That(offRoad.MaxSpeed, Is.LessThan(asphalt.MaxSpeed));
        }

        [Test]
        public void BoostAndSlipperySurfaces_UseDistinctBoundedProfiles()
        {
            var asphalt = ArcadeSurfaceResponse.For(ArcadeSurfaceType.Asphalt);
            var boost = ArcadeSurfaceResponse.For(ArcadeSurfaceType.Boost);
            var slippery = ArcadeSurfaceResponse.For(ArcadeSurfaceType.Slippery);

            Assert.That(boost.Acceleration, Is.GreaterThan(asphalt.Acceleration));
            Assert.That(boost.MaxSpeed, Is.GreaterThan(asphalt.MaxSpeed));
            Assert.That(slippery.Grip, Is.LessThan(asphalt.Grip));
            Assert.That(slippery.Acceleration, Is.LessThan(asphalt.Acceleration));
        }

        [Test]
        public void NitroEnergy_ConsumesWhileActiveAndRechargesWhileIdle()
        {
            var consuming = ArcadeNitroEnergy.Step(1f, true, 0.5f, 0.4f, 0.1f);
            Assert.That(consuming.Active, Is.True);
            Assert.That(consuming.Energy, Is.EqualTo(0.8f).Within(0.0001f));

            var exhausted = ArcadeNitroEnergy.Step(0.2f, true, 1f, 0.4f, 0.1f);
            Assert.That(exhausted.Active, Is.False);
            Assert.That(exhausted.Energy, Is.EqualTo(0f).Within(0.0001f));

            var recharged = ArcadeNitroEnergy.Step(0.4f, false, 1f, 0.4f, 0.2f);
            Assert.That(recharged.Energy, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void DriftEnergy_GainsFromSlipAndDecaysWhenNotDrifting()
        {
            var gained = ArcadeDriftEnergy.Step(0.2f, true, 0.5f, 1f, 0.4f, 0.1f);
            Assert.That(gained, Is.EqualTo(0.4f).Within(0.0001f));

            var decayed = ArcadeDriftEnergy.Step(gained, false, 1f, 1f, 0.4f, 0.1f);
            Assert.That(decayed, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void ImpactResponse_ClampsVirtualImpulseBudgetSymmetrically()
        {
            Assert.That(ArcadeImpactResponse.ClampVirtualImpulse(100f, 10f, 3f), Is.EqualTo(30f).Within(0.0001f));
            Assert.That(ArcadeImpactResponse.ClampVirtualImpulse(-100f, 10f, 3f), Is.EqualTo(-30f).Within(0.0001f));
            Assert.That(ArcadeImpactResponse.ClampVirtualImpulse(12f, 10f, 3f), Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void SafeAreaPolicy_NormalizesMarginsForLandscapeViewport()
        {
            var margins = SafeAreaLayoutPolicy.Normalize(1000f, 500f, 20f, 10f, 960f, 480f);

            Assert.That(margins.Left, Is.EqualTo(0.02f).Within(0.0001f));
            Assert.That(margins.Right, Is.EqualTo(0.02f).Within(0.0001f));
            Assert.That(margins.Top, Is.EqualTo(0.02f).Within(0.0001f));
            Assert.That(margins.Bottom, Is.EqualTo(0.02f).Within(0.0001f));
        }
    }
}
