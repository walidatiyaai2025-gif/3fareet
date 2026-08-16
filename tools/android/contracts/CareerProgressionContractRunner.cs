using System;
using Afareet.Progression;

internal static class CareerProgressionContractRunner
{
    private static int Main()
    {
        try
        {
            var service = new CareerProgressionService();
            var chapter = ChapterOneCareerContent.Build();
            var progress = CareerProgress.Empty();

            Require(service.CanEnter(chapter.Nodes[0], progress), "first node should be enterable at zero stars");
            Require(!service.CanEnter(chapter.Nodes[1], progress), "second node should remain locked below two stars");

            progress = service.CompleteNode(progress, chapter.Nodes[0].Id, 9);
            Require(progress.Stars == 3, "completion award must clamp to three stars");
            Require(progress.CompletedNodeIds.Count == 1 && progress.CompletedNodeIds[0] == "c01_r01", "first completion must be recorded exactly once");

            var duplicate = service.CompleteNode(progress, chapter.Nodes[0].Id, 3);
            Require(object.ReferenceEquals(progress, duplicate), "duplicate completion should be a no-op instance");
            Require(duplicate.Stars == 3, "duplicate completion must not farm stars");
            Require(service.CanEnter(chapter.Nodes[1], duplicate), "three stars should unlock the second node");

            var zeroAward = service.CompleteNode(duplicate, chapter.Nodes[1].Id, -100);
            Require(zeroAward.Stars == 3, "negative completion award must clamp to zero");
            Require(zeroAward.IsNodeCompleted(chapter.Nodes[1].Id), "zero-star completion must still complete the node");

            var claimed = service.Claim("reward_b", zeroAward);
            claimed = service.Claim("reward_a", claimed);
            var duplicateClaim = service.Claim("reward_a", claimed);
            Require(object.ReferenceEquals(claimed, duplicateClaim), "duplicate reward claim should be a no-op instance");
            Require(claimed.ClaimedRewardIds.Count == 2, "reward ids must remain unique");
            Require(claimed.ClaimedRewardIds[0] == "reward_a" && claimed.ClaimedRewardIds[1] == "reward_b", "reward ids must expose deterministic ordinal ordering");
            Require(!service.CanClaim("reward_a", claimed), "claimed reward must not be claimable again");

            var complete = claimed;
            for (var index = 0; index < chapter.Nodes.Count; index++)
                complete = service.CompleteNode(complete, chapter.Nodes[index].Id, 0);
            Require(service.ChapterComplete(chapter, complete), "chapter must complete after every node id is completed");

            Expect<ArgumentException>(() => service.CanClaim(" ", complete), "blank reward id must fail closed");
            Expect<ArgumentOutOfRangeException>(() => new CareerProgress(2, 0, Array.Empty<string>(), Array.Empty<string>()), "unsupported version must fail closed");
            Expect<ArgumentOutOfRangeException>(() => new CareerProgress(CareerProgress.CurrentVersion, -1, Array.Empty<string>(), Array.Empty<string>()), "negative stars must fail closed");

            var maxed = new CareerProgress(CareerProgress.CurrentVersion, int.MaxValue, Array.Empty<string>(), Array.Empty<string>());
            Expect<OverflowException>(() => service.CompleteNode(maxed, "overflow_node", 1), "star overflow must fail closed");

            Console.WriteLine("Career progression behavior contract passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Expect<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
