using System;
using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests.Vehicle
{
    public sealed class ArcadeDriveModifierTests
    {
        [Test]
        public void Neutral_IsInitializedAndPreservesEveryAxis()
        {
            var modifier = ArcadeDriveModifier.Neutral();

            Assert.That(modifier.IsValid, Is.True);
            Assert.That(modifier.IsNeutral, Is.True);
            Assert.That(modifier.AccelerationMultiplier, Is.EqualTo(1d));
            Assert.That(modifier.MaxSpeedMultiplier, Is.EqualTo(1d));
            Assert.That(modifier.SteeringAuthorityMultiplier, Is.EqualTo(1d));
            Assert.That(modifier.GripMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void Constructor_AcceptsInclusiveSafetyBounds()
        {
            var modifier = new ArcadeDriveModifier(
                ArcadeDriveModifier.MinimumMultiplier,
                ArcadeDriveModifier.MaximumMultiplier,
                ArcadeDriveModifier.MinimumMultiplier,
                ArcadeDriveModifier.MaximumMultiplier);

            Assert.That(modifier.IsValid, Is.True);
            Assert.That(modifier.IsNeutral, Is.False);
        }

        [TestCase(0d)]
        [TestCase(.249d)]
        [TestCase(2.001d)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Constructor_RejectsUnsafeAccelerationMultiplier(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArcadeDriveModifier(value, 1d, 1d, 1d));
        }

        [Test]
        public void Constructor_RejectsNaN()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArcadeDriveModifier(double.NaN, 1d, 1d, 1d));
        }

        [Test]
        public void DefaultStruct_FailsClosed()
        {
            var modifier = default(ArcadeDriveModifier);

            Assert.That(modifier.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() =>
                ArcadeDriveModifier.ValidateInitialized(modifier, nameof(modifier)));
        }
    }
}
