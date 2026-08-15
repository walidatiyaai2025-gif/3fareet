using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class VehicleHandlingSurfaceTests
    {
        [Test]
        public void TractionLeavesDriveUntouchedBelowSlipThreshold()
        {
            var drive = VehicleHandlingPolicy.LimitDriveForTraction(1f, 2f, 3f, .6f);
            Assert.That(drive, Is.EqualTo(1f));
        }

        [Test]
        public void TractionReducesDriveAboveSlipThreshold()
        {
            var drive = VehicleHandlingPolicy.LimitDriveForTraction(1f, 6f, 3f, .5f);
            Assert.That(drive, Is.EqualTo(.5f).Within(.0001f));
        }

        [Test]
        public void DriftBlendRequiresRequestSteerAndSlip()
        {
            Assert.That(VehicleHandlingPolicy.DriftBlend(false, 1f, 6f, .2f, 6f), Is.EqualTo(0f));
            Assert.That(VehicleHandlingPolicy.DriftBlend(true, .1f, 6f, .2f, 6f), Is.EqualTo(0f));
            Assert.That(VehicleHandlingPolicy.DriftBlend(true, 1f, 0f, .2f, 6f), Is.EqualTo(0f));
            Assert.That(VehicleHandlingPolicy.DriftBlend(true, 1f, 6f, .2f, 6f), Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void EffectiveGripBlendsAndAppliesSurfaceMultiplier()
        {
            var roadGrip = VehicleHandlingPolicy.EffectiveGrip(8f, 2f, .5f, 1f);
            var offRoadGrip = VehicleHandlingPolicy.EffectiveGrip(8f, 2f, .5f, .5f);
            Assert.That(roadGrip, Is.EqualTo(5f).Within(.0001f));
            Assert.That(offRoadGrip, Is.EqualTo(2.5f).Within(.0001f));
        }

        [Test]
        public void ConfigSurfaceProfilesReduceOffRoadAndIncreaseBoostSpeed()
        {
            var config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            try
            {
                var offRoad = config.SurfaceResponseFor(ArcadeSurfaceKind.OffRoad);
                var boost = config.SurfaceResponseFor(ArcadeSurfaceKind.Boost);
                Assert.That(offRoad.GripMultiplier, Is.LessThan(1f));
                Assert.That(offRoad.AccelerationMultiplier, Is.LessThan(1f));
                Assert.That(offRoad.MaxSpeedMultiplier, Is.LessThan(1f));
                Assert.That(boost.AccelerationMultiplier, Is.GreaterThan(1f));
                Assert.That(boost.MaxSpeedMultiplier, Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ConfigRejectsInvalidTractionAndSurfaceTuning()
        {
            var config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            try
            {
                config.tractionSlipThresholdMetersPerSecond = 0f;
                Assert.That(config.IsValid(out var tractionError), Is.False);
                StringAssert.Contains("Traction", tractionError);

                config.tractionSlipThresholdMetersPerSecond = 3f;
                config.offRoadGripMultiplier = 0f;
                Assert.That(config.IsValid(out var surfaceError), Is.False);
                StringAssert.Contains("Surface", surfaceError);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ExistingCairoRoadAndDesertNamesClassifyDeterministically()
        {
            var road = new GameObject("Road 07");
            var desert = new GameObject("Desert Ground");
            try
            {
                var roadCollider = road.AddComponent<BoxCollider>();
                var desertCollider = desert.AddComponent<BoxCollider>();
                Assert.That(ArcadeGroundSurfaceSensor.Classify(roadCollider), Is.EqualTo(ArcadeSurfaceKind.Asphalt));
                Assert.That(ArcadeGroundSurfaceSensor.Classify(desertCollider), Is.EqualTo(ArcadeSurfaceKind.OffRoad));
            }
            finally
            {
                Object.DestroyImmediate(road);
                Object.DestroyImmediate(desert);
            }
        }

        [Test]
        public void ExplicitMarkerOverridesNameHeuristics()
        {
            var road = new GameObject("Road With Slippery Patch");
            try
            {
                var collider = road.AddComponent<BoxCollider>();
                road.AddComponent<ArcadeSurfaceMarker>().Configure(ArcadeSurfaceKind.Slippery);
                Assert.That(ArcadeGroundSurfaceSensor.Classify(collider), Is.EqualTo(ArcadeSurfaceKind.Slippery));
            }
            finally
            {
                Object.DestroyImmediate(road);
            }
        }

        [Test]
        public void GroundProbeIgnoresVehicleColliderAndFindsDesertBelow()
        {
            var car = new GameObject("Probe Car");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                car.transform.position = new Vector3(0f, .7f, 0f);
                car.AddComponent<BoxCollider>().size = new Vector3(1.5f, .8f, 3f);
                var sensor = car.AddComponent<ArcadeGroundSurfaceSensor>();
                sensor.ConfigureProbe(.25f, 2f, ~0);

                ground.name = "Desert Ground";
                ground.transform.position = new Vector3(0f, -.25f, 0f);
                ground.transform.localScale = new Vector3(10f, .5f, 10f);
                Physics.SyncTransforms();

                sensor.ProbeNow();

                Assert.That(sensor.IsGrounded, Is.True);
                Assert.That(sensor.GroundCollider, Is.EqualTo(ground.GetComponent<Collider>()));
                Assert.That(sensor.CurrentSurface, Is.EqualTo(ArcadeSurfaceKind.OffRoad));
            }
            finally
            {
                Object.DestroyImmediate(car);
                Object.DestroyImmediate(ground);
            }
        }
    }
}
