using System;
using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class WheelColliderSuspensionPrototypeTests
    {
        [Test]
        public void SuspensionMathSpringScalesWithFrequencySquared()
        {
            var low = WheelSuspensionMath.Calculate(1200f, 4, 4f, .8f);
            var high = WheelSuspensionMath.Calculate(1200f, 4, 8f, .8f);
            Assert.That(high.SpringRate / low.SpringRate, Is.EqualTo(4f).Within(.001f));
        }

        [Test]
        public void SuspensionMathDamperScalesWithDampingRatio()
        {
            var low = WheelSuspensionMath.Calculate(1200f, 4, 5f, .5f);
            var high = WheelSuspensionMath.Calculate(1200f, 4, 5f, 1f);
            Assert.That(high.DamperRate / low.DamperRate, Is.EqualTo(2f).Within(.001f));
        }

        [Test]
        public void SuspensionMathRejectsInvalidInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelSuspensionMath.Calculate(0f, 4, 5f, .8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelSuspensionMath.Calculate(1200f, 0, 5f, .8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelSuspensionMath.Calculate(1200f, 4, 0f, .8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => WheelSuspensionMath.Calculate(1200f, 4, 5f, 0f));
        }

        [Test]
        public void PrototypeAutoDiscoversChildWheelsAndAppliesTuning()
        {
            var root = new GameObject("Suspension Test Root");
            try
            {
                var body = root.AddComponent<Rigidbody>();
                body.mass = 1200f;
                var wheels = CreateWheels(root.transform, 4);
                var prototype = root.AddComponent<WheelColliderSuspensionPrototype>();
                prototype.ConfigurePrototype(Array.Empty<WheelCollider>(), 5f, .8f, .24f, .42f, .12f, .3f);

                var coefficients = prototype.ApplyPrototypeTuning();

                Assert.That(prototype.WheelCount, Is.EqualTo(4));
                Assert.That(coefficients.SpringRate, Is.GreaterThan(0f));
                Assert.That(coefficients.DamperRate, Is.GreaterThan(0f));
                foreach (var wheel in wheels)
                {
                    Assert.That(wheel.suspensionSpring.spring, Is.EqualTo(coefficients.SpringRate).Within(.1f));
                    Assert.That(wheel.suspensionSpring.damper, Is.EqualTo(coefficients.DamperRate).Within(.1f));
                    Assert.That(wheel.suspensionSpring.targetPosition, Is.EqualTo(.42f).Within(.0001f));
                    Assert.That(wheel.suspensionDistance, Is.EqualTo(.24f).Within(.0001f));
                    Assert.That(wheel.forceAppPointDistance, Is.EqualTo(.12f).Within(.0001f));
                    Assert.That(wheel.wheelDampingRate, Is.EqualTo(.3f).Within(.0001f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PrototypeRejectsDuplicateWheelReferences()
        {
            var root = new GameObject("Duplicate Wheel Root");
            try
            {
                root.AddComponent<Rigidbody>().mass = 1000f;
                var wheel = new GameObject("Wheel").AddComponent<WheelCollider>();
                wheel.transform.SetParent(root.transform, false);
                var prototype = root.AddComponent<WheelColliderSuspensionPrototype>();
                prototype.ConfigurePrototype(new[] { wheel, wheel }, 5f, .8f, .2f, .5f, .1f, .25f);

                Assert.That(prototype.IsValid(out var error), Is.False);
                StringAssert.Contains("only be supplied once", error);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PrototypeRejectsMissingWheelColliders()
        {
            var root = new GameObject("No Wheels Root");
            try
            {
                root.AddComponent<Rigidbody>().mass = 1000f;
                var prototype = root.AddComponent<WheelColliderSuspensionPrototype>();
                prototype.ConfigurePrototype(Array.Empty<WheelCollider>(), 5f, .8f, .2f, .5f, .1f, .25f);

                Assert.That(prototype.IsValid(out var error), Is.False);
                StringAssert.Contains("At least one WheelCollider", error);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static WheelCollider[] CreateWheels(Transform parent, int count)
        {
            var wheels = new WheelCollider[count];
            for (var i = 0; i < count; i++)
            {
                var wheelObject = new GameObject($"Wheel {i}");
                wheelObject.transform.SetParent(parent, false);
                wheels[i] = wheelObject.AddComponent<WheelCollider>();
            }
            return wheels;
        }
    }
}
