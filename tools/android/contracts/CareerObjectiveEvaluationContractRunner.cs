using System;
using System.Collections.Generic;
using Afareet.Progression;

internal static class CareerObjectiveEvaluationContractRunner
{
    private static int Main()
    {
        try
        {
            var first = FindDefinition("c01_r01");
            var firstFinish = CareerObjectiveEvaluationPolicy.Evaluate(
                first,
                new CareerEventOutcome(finished: true, restartCount: 0));
            Require(firstFinish.Entries.Count == 1, "first event must expose one objective");
            Require(firstFinish.Entries[0].ObjectiveId == "finish_c01_r01", "first finish id drifted");
            Require(firstFinish.AllCompleted, "finished first event must complete finish objective");

            var second = FindDefinition("c01_r02");
            var cleanFinish = CareerObjectiveEvaluationPolicy.Evaluate(
                second,
                new CareerEventOutcome(finished: true, restartCount: 0));
            Require(cleanFinish.CompletedCount == 2, "clean finish must complete both objectives");
            Require(cleanFinish.Entries[0].ObjectiveId == "finish_c01_r02", "finish ordering drifted");
            Require(cleanFinish.Entries[1].ObjectiveId == "clean_c01_r02", "clean ordering drifted");

            var restartedFinish = CareerObjectiveEvaluationPolicy.Evaluate(
                second,
                new CareerEventOutcome(finished: true, restartCount: 2));
            Require(restartedFinish.CompletedCount == 1, "restart must block clean objective only");
            Require(restartedFinish.Entries[0].IsComplete, "restart must not block finish objective");
            Require(!restartedFinish.Entries[1].IsComplete, "restart must block clean objective");

            var unfinished = CareerObjectiveEvaluationPolicy.Evaluate(
                second,
                new CareerEventOutcome(finished: false, restartCount: 0));
            Require(unfinished.CompletedCount == 0, "unfinished event must complete no objective");

            RequireThrows<ArgumentOutOfRangeException>(
                () => new CareerEventOutcome(finished: false, restartCount: -1),
                "negative restart count must fail closed");

            var unknown = new CareerNodeDefinition(
                second.Node,
                new[] { new CareerObjective("future_c01_r02", "Future rule", 1d) },
                second.Rewards);
            RequireThrows<InvalidOperationException>(
                () => CareerObjectiveEvaluationPolicy.Evaluate(
                    unknown,
                    new CareerEventOutcome(finished: true, restartCount: 0)),
                "unknown objective must fail closed");

            var nonBinary = new CareerNodeDefinition(
                second.Node,
                new[] { new CareerObjective("finish_c01_r02", "Finish twice", 2d) },
                second.Rewards);
            RequireThrows<InvalidOperationException>(
                () => CareerObjectiveEvaluationPolicy.Evaluate(
                    nonBinary,
                    new CareerEventOutcome(finished: true, restartCount: 0)),
                "non-binary target must fail closed");

            Console.WriteLine("Career objective evaluation contract passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static CareerNodeDefinition FindDefinition(string nodeId)
    {
        var definitions = ChapterOneCareerEventContent.CreateDefinitions();
        for (var index = 0; index < definitions.Count; index++)
            if (StringComparer.Ordinal.Equals(definitions[index].Node.Id, nodeId))
                return definitions[index];

        throw new KeyNotFoundException(nodeId);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
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
