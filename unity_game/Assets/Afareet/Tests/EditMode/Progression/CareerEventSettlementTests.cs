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
            var progress = CareerProgress.Empty();

            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0),
                progress);

            Assert.That(settlement.NodeCompletedNow, Is.True);
            Assert.That(settlement.StarsEarned, Is.EqualTo(3));
            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(settlement.Progress.IsNodeCompleted(definition.Node.Id), Is.True);
            Assert.That(settlement.GrantedRewards.Count, Is.EqualTo(definition.Rewards.Count));
            Assert.That(settlement.CoinsGranted, Is.EqualTo(250));
            Assert.That(settlement.SpiritGranted, Is.EqualTo(5));
            Assert.That(
                settlement.Progress.IsRewardClaimed(
                    CareerEventSettlementService.BuildRewardId(definition.Node.Id, 0)),
                Is.True);

            var replay = service.Settle(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0),
                settlement.Progress);

            Assert.That(replay.NodeCompletedNow, Is.False);
            Assert.That(replay.StarsEarned, Is.Zero);
            Assert.That(replay.GrantedAnyReward, Is.False);
            Assert.That(replay.Progress, Is.SameAs(settlement.Progress));
        }

        [Test]
        public void Settle_FinishedAfterRestart_GrantsTwoStars()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[1];
            var service = new CareerEventSettlementService();

            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 1),
                CareerProgress.Empty());

            Assert.That(settlement.Evaluation.AllCompleted, Is.False);
            Assert.That(settlement.Evaluation.CompletedCount, Is.EqualTo(1));
            Assert.That(settlement.StarsEarned, Is.EqualTo(2));
            Assert.That(settlement.Progress.Stars, Is.EqualTo(2));
        }

        [Test]
        public void Settle_FailedEvent_DoesNotCompleteOrGrant()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[0];
            var service = new CareerEventSettlementService();

            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(finished: false, restartCount: 0),
                CareerProgress.Empty());

            Assert.That(settlement.NodeCompletedNow, Is.False);
            Assert.That(settlement.StarsEarned, Is.Zero);
            Assert.That(settlement.GrantedAnyReward, Is.False);
            Assert.That(settlement.Progress.Stars, Is.Zero);
            Assert.That(settlement.Progress.IsNodeCompleted(definition.Node.Id), Is.False);
        }

        [Test]
        public void Settle_CompletedButUnclaimedProgress_RecoversRewardsWithoutStars()
        {
            var definition = ChapterOneCareerEventContent.CreateDefinitions()[0];
            var progression = new CareerProgressionService();
            var completedWithoutClaim = progression.CompleteNode(
                CareerProgress.Empty(),
                definition.Node.Id,
                3);
            var service = new CareerEventSettlementService();

            var settlement = service.Settle(
                definition,
                new CareerEventOutcome(finished: true, restartCount: 0),
                completedWithoutClaim);

            Assert.That(settlement.NodeCompletedNow, Is.False);
            Assert.That(settlement.StarsEarned, Is.Zero);
            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(settlement.GrantedAnyReward, Is.True);
            Assert.That(settlement.CoinsGranted, Is.EqualTo(250));
            Assert.That(
                settlement.Progress.IsRewardClaimed(
                    CareerEventSettlementService.BuildRewardId(definition.Node.Id, 0)),
                Is.True);
        }

        [Test]
        public void PerfectFirstEvent_UnlocksSecondChapterNode()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var chapter = ChapterOneCareerContent.CreateFoundation();
            var settlementService = new CareerEventSettlementService();
            var progression = new CareerProgressionService();

            var settlement = settlementService.Settle(
                definitions[0],
                new CareerEventOutcome(finished: true, restartCount: 0),
                CareerProgress.Empty());

            Assert.That(settlement.Progress.Stars, Is.EqualTo(3));
            Assert.That(progression.CanEnter(chapter.Nodes[1], settlement.Progress), Is.True);
        }

        [Test]
        public void Settle_BossEvent_ReportsVehicleUnlockPayload()
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var boss = definitions[definitions.Count - 1];
            var service = new CareerEventSettlementService();

            var settlement = service.Settle(
                boss,
                new CareerEventOutcome(finished: true, restartCount: 0),
                CareerProgress.Empty());

            Assert.That(settlement.UnlockedVehicleIds, Does.Contain("djinn_spirit"));
            Assert.That(settlement.GrantedRewardIds.Count, Is.EqualTo(boss.Rewards.Count));
        }
    }
}
