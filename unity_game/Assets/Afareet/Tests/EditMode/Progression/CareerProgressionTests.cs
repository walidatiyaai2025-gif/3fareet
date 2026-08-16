using System;
using NUnit.Framework;

namespace Afareet.Tests.Progression
{
    public sealed class CareerProgressionTests
    {
        [Test]
        public void CompleteNode_ClampsStarsAndRejectsDuplicateStarFarming()
        {
            var service = new CareerProgressionService();
            var progress = CareerProgress.Empty();

            progress = service.CompleteNode(progress, "c01_r01", 9);
            Assert.That(progress.Stars, Is.EqualTo(3));
            Assert.That(progress.CompletedNodeIds, Is.EqualTo(new[] { "c01_r01" }));

            var duplicate = service.CompleteNode(progress, "c01_r01", 3);
            Assert.That(duplicate, Is.SameAs(progress));
            Assert.That(duplicate.Stars, Is.EqualTo(3));
            Assert.That(duplicate.CompletedNodeIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void CanEnter_UsesRequiredStarsFromCareerDefinition()
        {
            var service = new CareerProgressionService();
            var chapter = ChapterOneCareerContent.CreateFoundation();
            var progress = CareerProgress.Empty();

            Assert.That(service.CanEnter(chapter.Nodes[0], progress), Is.True);
            Assert.That(service.CanEnter(chapter.Nodes[1], progress), Is.False);

            progress = service.CompleteNode(progress, chapter.Nodes[0].Id, 3);
            Assert.That(service.CanEnter(chapter.Nodes[1], progress), Is.True);
        }

        [Test]
        public void Claim_IsIdempotentAndDeterministic()
        {
            var service = new CareerProgressionService();
            var progress = CareerProgress.Empty();

            var once = service.Claim("reward_b", progress);
            once = service.Claim("reward_a", once);
            var twice = service.Claim("reward_a", once);

            Assert.That(twice, Is.SameAs(once));
            Assert.That(once.ClaimedRewardIds, Is.EqualTo(new[] { "reward_a", "reward_b" }));
            Assert.That(service.CanClaim("reward_a", once), Is.False);
            Assert.Throws<ArgumentException>(() => service.CanClaim(" ", once));
        }

        [Test]
        public void ChapterComplete_RequiresEveryChapterNode()
        {
            var service = new CareerProgressionService();
            var chapter = ChapterOneCareerContent.CreateFoundation();
            var progress = CareerProgress.Empty();

            Assert.That(service.ChapterComplete(chapter, progress), Is.False);
            foreach (var node in chapter.Nodes)
                progress = service.CompleteNode(progress, node.Id, 0);
            Assert.That(service.ChapterComplete(chapter, progress), Is.True);
        }

        [Test]
        public void Constructor_ValidatesVersionStarsAndIds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CareerProgress(99, 0, Array.Empty<string>(), Array.Empty<string>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CareerProgress(CareerProgress.CurrentVersion, -1, Array.Empty<string>(), Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() =>
                new CareerProgress(CareerProgress.CurrentVersion, 0, new[] { "" }, Array.Empty<string>()));
        }

        [Test]
        public void CompleteNode_FailsClosedOnIntegerOverflow()
        {
            var service = new CareerProgressionService();
            var progress = new CareerProgress(
                CareerProgress.CurrentVersion,
                int.MaxValue,
                Array.Empty<string>(),
                Array.Empty<string>());

            Assert.Throws<OverflowException>(() => service.CompleteNode(progress, "c01_r01", 1));
        }
    }
}
