using System;
using System.Collections.Generic;
using Afareet.Race;

internal static class RaceRankingContractRunner
{
    private static int Main()
    {
        try
        {
            OrderingContract();
            InputImmutabilityContract();
            DuplicateIdentityContract();
            FinishedRacerContract();
            Console.WriteLine("Race ranking behavior contract: PASS");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void OrderingContract()
    {
        var input = new List<RaceProgressSnapshot>
        {
            Progress("near", checkpoints: 1, segment: .99f, stable: 0),
            Progress("ahead", checkpoints: 2, segment: .05f, stable: 1),
            Progress("same-checkpoint-farther", checkpoints: 2, segment: .80f, stable: 2)
        };

        var ranked = RaceRanking.Rank(input);
        Require(ranked.Count == 3, "Expected three ranked racers.");
        Require(ranked[0].Progress.RacerId == "same-checkpoint-farther", "Segment tie-break order changed.");
        Require(ranked[0].Position == 1, "Winner position must be one-based.");
        Require(ranked[1].Progress.RacerId == "ahead", "Checkpoint ranking order changed.");
        Require(ranked[2].Progress.RacerId == "near", "Lower checkpoint count must remain behind.");
    }

    private static void InputImmutabilityContract()
    {
        var input = new List<RaceProgressSnapshot>
        {
            Progress("behind", checkpoints: 1, stable: 0),
            Progress("ahead", checkpoints: 3, stable: 1),
            Progress("middle", checkpoints: 2, stable: 2)
        };

        _ = RaceRanking.Rank(input);
        Require(input[0].RacerId == "behind", "Rank mutated caller input at index 0.");
        Require(input[1].RacerId == "ahead", "Rank mutated caller input at index 1.");
        Require(input[2].RacerId == "middle", "Rank mutated caller input at index 2.");
    }

    private static void DuplicateIdentityContract()
    {
        try
        {
            _ = RaceRanking.Rank(new List<RaceProgressSnapshot>
            {
                Progress("duplicate", stable: 0),
                Progress("duplicate", stable: 1)
            });
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException("Duplicate racer ids must fail closed.");
    }

    private static void FinishedRacerContract()
    {
        var ranked = RaceRanking.Rank(new List<RaceProgressSnapshot>
        {
            Progress("running", checkpoints: 99, segment: 1f, stable: 0),
            Finished("second", 12f, stable: 1),
            Finished("winner", 10f, stable: 2)
        });

        Require(ranked[0].Progress.RacerId == "winner", "Lower finish time must win.");
        Require(ranked[1].Progress.RacerId == "second", "Finished racers must precede active racers.");
        Require(ranked[2].Progress.RacerId == "running", "Active racer must remain behind finished racers.");
    }

    private static RaceProgressSnapshot Progress(
        string id,
        int laps = 0,
        int checkpoints = 0,
        float segment = 0f,
        int stable = 0)
    {
        return new RaceProgressSnapshot(id, false, laps, checkpoints, segment, -1f, stable);
    }

    private static RaceProgressSnapshot Finished(string id, float finishTime, int stable)
    {
        return new RaceProgressSnapshot(id, true, 1, 4, 1f, finishTime, stable);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
