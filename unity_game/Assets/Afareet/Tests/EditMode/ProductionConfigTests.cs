using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class ProductionConfigTests
    {
        [Test]
        public void ArcadeCarDefaultsAreValidAndDriftGripIsLowerThanRoadGrip()
        {
            var config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            try
            {
                Assert.That(config.IsValid(out var error), Is.True, error);
                Assert.That(config.driftGrip, Is.LessThan(config.grip));
                Assert.That(config.maxSpeedMetersPerSecond, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ArcadeCarRejectsInvalidDriftGrip()
        {
            var config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            try
            {
                config.driftGrip = config.grip;
                Assert.That(config.IsValid(out var error), Is.False);
                StringAssert.Contains("Drift grip", error);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ChaseCameraDefaultsAreValidAndNitroWidensFov()
        {
            var config = ScriptableObject.CreateInstance<ChaseCameraConfig>();
            try
            {
                Assert.That(config.IsValid(out var error), Is.True, error);
                Assert.That(config.offset.z, Is.LessThan(0f));
                Assert.That(config.nitroFieldOfView, Is.GreaterThan(config.normalFieldOfView));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ChaseCameraRejectsForwardOffset()
        {
            var config = ScriptableObject.CreateInstance<ChaseCameraConfig>();
            try
            {
                config.offset = Vector3.forward;
                Assert.That(config.IsValid(out var error), Is.False);
                StringAssert.Contains("behind", error);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PlayerInputIsAppliedDirectlyAndClamped()
        {
            var carObject = new GameObject("Input Test Car");
            try
            {
                carObject.AddComponent<Rigidbody>();
                var car = carObject.AddComponent<ArcadeCarController>();
                car.SetPlayerInput(2f, -2f, false, true, true);

                Assert.That(car.CurrentThrottleInput, Is.EqualTo(1f));
                Assert.That(car.CurrentSteerInput, Is.EqualTo(-1f));
                Assert.That(car.CurrentBrakeInput, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(carObject);
            }
        }
    }
}
