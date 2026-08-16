using System;
using System.Linq;
using Afareet.Race;
using NUnit.Framework;

namespace Afareet.Tests.Race
{
    public sealed class RaceRewardSettlementTests
    {
        [Test]
        public void PrototypeEnchantedPound_RetainsLegacyDoubleRewardParity()
        {
            var rule = PowerUpRuntimeDefaults.CreatePrototypeRuleset()
                .Snapshot()
                .Single(value => value.Kind == PowerUpKind.EnchantedPound);

            Assert.That(rule.EffectSpec.Magnitude, Is.EqualTo(2d));
        }

        [Test]
        public void NeutralSettlement_PreservesBaseReward()
        {
            var settlement = RaceRewardSettlementPolicy.Settle(250, 1d);

            Assert.That(settlement.BaseRewardUnits, Is.EqualTo(250));
            Assert.That(settlement.SettledRewardUnits, Is.EqualTo(250));
            Assert.That(settlement.BonusRewardUnits, Is.Zero);
            Assert.That(settlement.WasModified, Is.False);
        }

        [Test]
        public void ActiveEnchantedPound_DoublesRewardAndSnapshotSurvivesRuntimeReset()
        {
            var runtime = Runtime();
            var use = runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 10d);
            Assert.That(use.Status, Is.EqualTo(PowerUpRuntimeUseStatus.Used));

            var snapshot = runtime.CaptureRewardSettlementSnapshot("player", 11d);
            runtime.ResetRace();
            var settlement = snapshot.Settle(350);

            Assert.That(snapshot.RewardMultiplier, Is.EqualTo(2d));
            Assert.That(settlement.SettledRewardUnits, Is.EqualTo(700));
            Assert.That(settlement.BonusRewardUnits, Is.EqualTo(350));
        }

        [Test]
        public void ExpiredEnchantedPound_ReturnsToNeutralSettlement()
        {
            var runtime = Runtime();
            runtime.TryUse("player", PowerUpKind.EnchantedPound, null, 0d);

            var settlement = runtime.SettleReward("player", 500, 8.1d);

            Assert.That(settlement.RewardMultiplier, Is.EqualTo(1d));
            Assert.That(settlement.SettledRewardUnits, Is.EqualTo(500));
        }

        [Test]
        public void FractionalSettlement_UsesMidpointAwayFromZero()
        {
            var settlement = RaceRewardSettlementPolicy.Settle(3, 1.5d);
            Assert.That(settlement.SettledRewardUnits, Is.EqualTo(5));
        }

        [Test]
        public void InvalidInputAndOverflow_FailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(-1, 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, .99d));
            Assert.Throws<ArgumentOutOfRangeException>(() => RaceRewardSettlementPolicy.Settle(1, 5.01d));
            Assert.Throws<OverflowException>(() => RaceRewardSettlementPolicy.Settle(int.MaxValue, 2d));
        }

        private static PowerUpRaceRuntime Runtime()
        {
            return new PowerUpRaceRuntime(
                PowerUpRuntimeDefaults.CreatePrototypeRuleset(),
                new[] { new PowerUpRacerRegistration("player") });
        }
    }
}
