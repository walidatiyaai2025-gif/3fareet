using Afareet.Vehicle;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests
{
    public sealed class VehicleSpiritPolicyTests
    {
        [Test]
        public void NitroForceCurveRisesWithSpeedAndCapsAtOne()
        {
            var low = VehicleSpiritPolicy.NitroForceScale(0f, 50f, 0.65f, 0.7f);
            var middle = VehicleSpiritPolicy.NitroForceScale(20f, 50f, 0.65f, 0.7f);
            var high = VehicleSpiritPolicy.NitroForceScale(50f, 50f, 0.65f, 0.7f);

            Assert.That(low, Is.EqualTo(0.65f).Within(0.0001f));
            Assert.That(middle, Is.GreaterThan(low));
            Assert.That(high, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void NitroActivationIsBlockedDuringCooldown()
        {
            Assert.That(VehicleSpiritPolicy.CanActivateNitro(true, 1f, 0.2f, 80f, 0.1f, 15f), Is.False);
        }

        [Test]
        public void NitroActivationRequiresEnergyAndMinimumSpeed()
        {
            Assert.That(VehicleSpiritPolicy.CanActivateNitro(true, 0.09f, 0f, 80f, 0.1f, 15f), Is.False);
            Assert.That(VehicleSpiritPolicy.CanActivateNitro(true, 0.5f, 0f, 10f, 0.1f, 15f), Is.False);
            Assert.That(VehicleSpiritPolicy.CanActivateNitro(true, 0.5f, 0f, 80f, 0.1f, 15f), Is.True);
        }

        [Test]
        public void NitroEnergyDoesNotRechargeWhileCooldownBlocksRecharge()
        {
            var energy = VehicleSpiritPolicy.StepNitroEnergy(0.4f, false, false, 1f, 0.2f, 0.1f);
            Assert.That(energy, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void NitroEnergyRechargesWhenAllowedAndClampsAtOne()
        {
            var energy = VehicleSpiritPolicy.StepNitroEnergy(0.95f, false, true, 1f, 0.2f, 0.1f);
            Assert.That(energy, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void DriftChargeRequiresSteerSpeedAndLateralSlip()
        {
            Assert.That(VehicleSpiritPolicy.CanChargeDrift(true, 0.5f, 70f, 2f, 0.2f, 30f, 0.8f), Is.True);
            Assert.That(VehicleSpiritPolicy.CanChargeDrift(true, 0.1f, 70f, 2f, 0.2f, 30f, 0.8f), Is.False);
            Assert.That(VehicleSpiritPolicy.CanChargeDrift(true, 0.5f, 20f, 2f, 0.2f, 30f, 0.8f), Is.False);
            Assert.That(VehicleSpiritPolicy.CanChargeDrift(true, 0.5f, 70f, 0.2f, 0.2f, 30f, 0.8f), Is.False);
        }

        [Test]
        public void DriftEnergyChargesFromEligibleSlip()
        {
            var energy = VehicleSpiritPolicy.StepDriftEnergy(
                0.2f, true, 0.6f, 80f, 6f, 1f,
                0.25f, 0.1f, 0.2f, 30f, 0.8f, 6f);

            Assert.That(energy, Is.GreaterThan(0.2f));
        }

        [Test]
        public void DriftEnergyDecaysWhenDriftIsIneligible()
        {
            var energy = VehicleSpiritPolicy.StepDriftEnergy(
                0.5f, false, 0f, 80f, 0f, 1f,
                0.25f, 0.1f, 0.2f, 30f, 0.8f, 6f);

            Assert.That(energy, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void SpiritConfigDefaultsAreValidAndThresholdsAreOrdered()
        {
            var config = ScriptableObject.CreateInstance<ArcadeCarConfig>();
            try
            {
                Assert.That(config.IsValid(out var error), Is.True, error);
                Assert.That(config.nitroMinimumActivationEnergy, Is.InRange(0.01f, 1f));
                Assert.That(config.nitroCooldownSeconds, Is.GreaterThanOrEqualTo(0f));
                Assert.That(config.driftEnergyFullGainSlipMetersPerSecond, Is.GreaterThan(config.driftEnergyMinimumSlipMetersPerSecond));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
