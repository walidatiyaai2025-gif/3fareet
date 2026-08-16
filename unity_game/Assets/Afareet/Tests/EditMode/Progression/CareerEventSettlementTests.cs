using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerEventSettlementTests
    {
        [Test]
        public void Settle_PerfectFirstEvent_GrantsThreeStarsAndRewardsOnce()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[0];
            var service = new CareerEventSettlementService();
            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(true, 0),
                CareerProgress.Empty());

            Assert.That(settlement.NodeCompletedNow, Is.True);
            Assert.That(settlement.StarsEarned, Is.EqualTo(3));
            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(settlement.GrantedRewards.Count, Is.EqualTo(definition.Rewards.Count));
            Assert.That(settlement.CoinsGranted, Is.EqualTo(250));
            Assert.That(settlement.SpiritGranted, Is.EqualTo(5));

            var replay = service.Settle(definition, new CareerEventOutcome(true, 0), settlement.Progress);
            Assert.That(replay.NodeCompletedNow, Is.False);
            Assert.That(replay.StarsEarned, Is.Zero);
            Assert.That(replay.GrantedAnyReward, Is.False);
            Assert.That(replay.Progress, Is.SameAs(settlement.Progress));
        }

        [Test]
        public void Settle_PassedTimeTrialAfterRestart_GrantsTwoStars()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[1];
            var service = new CareerEventSettlementService();
            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(true, 1, finishTimeSeconds: 90d),
                CareerProgress.Empty());

            Assert.That(settlement.Evaluation.CompletedCount, Is.EqualTo(2));
            Assert.That(settlement.Evaluation.AllCompleted, Is.False);
            Assert.That(settlement.NodeCompletedNow, Is.True);
            Assert.That(settlement.StarsEarned, Is.EqualTo(2));
            Assert.That(settlement.Progress.Stars, Is.EqualTo(2));
        }

        [Test]
        public void Settle_FailedModeGate_DoesNotCompleteOrGrant()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var service = new CareerEventSettlementService();

            var slowTimeTrial = service.Settle(
                definitions[1],
                new CareerEventOutcome(true, 0, finishTimeSeconds: 100d),
                CareerProgress.Empty());
            Assert.That(slowTimeTrial.NodeCompletedNow, Is.False);
            Assert.That(slowTimeTrial.GrantedAnyReward, Is.False);

            var lostBoss = service.Settle(
                definitions[4],
                new CareerEventOutcome(true, 0, finalPosition: 2),
                CareerProgress.Empty());
            Assert.That(lostBoss.NodeCompletedNow, Is.False);
            Assert.That(lostBoss.GrantedAnyReward, Is.False);
            Assert.That(lostBoss.UnlockedVehicleIds, Is.Empty);
        }

        [Test]
        public void Settle_CompletedButUnclaimedProgress_RecoversRewardsWithoutStars()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[0];
            var progression = new CareerProgressionService();
            var completedWithoutClaim = progression.CompleteNode(CareerProgress.Empty(), definition.Node.Id, 3);
            var service = new CareerEventSettlementService();

            var settlement = service.Settle(definition, new CareerEventOutcome(true, 0), completedWithoutClaim);

            Assert.That(settlement.NodeCompletedNow, Is.False);
            Assert.That(settlement.StarsEarned, Is.Zero);
            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(settlement.GrantedAnyReward, Is.True);
            Assert.That(settlement.CoinsGranted, Is.EqualTo(250));
        }

        [Test]
        public void PerfectFirstEvent_UnlocksSecondChapterNode()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var chapter = ChapterOneCareerContent.CreateFoundation();
            var settlement = new CareerEventSettlementService().Settle(
                definitions[0],
                new CareerEventOutcome(true, 0),
                CareerProgress.Empty());

            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(new CareerProgressionService().CanEnter(chapter.Nodes[1], settlement.Progress), Is.True);
        }

        [Test]
        public void Settle_WonBossEvent_ReportsVehicleUnlockPayload()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var boss = definitions[definitions.Count - 1];
            var settlement = new CareerEventSettlementService().Settle(
                boss,
                new CareerEventOutcome(true, 0, finalPosition: 1),
                CareerProgress.Empty());

            Assert.That(settlement.NodeCompletedNow, Is.True);
            Assert.That(settlement.UnlockedVehicleIds, Does.Contain("djinn_spirit"));
            Assert.That(settlement.GrantedRewardIds.Count, Is.EqualTo(boss.Rewards.Count));
        }
    }
}
