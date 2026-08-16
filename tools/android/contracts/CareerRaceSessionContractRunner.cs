using System;
using Afareet.CareerRuntime;
using Afareet.Progression;

internal static class CareerRaceSessionContractRunner
{
    private sealed class FakeRaceEventSource : ICareerRaceEventSource
    {
        public event Action<float> ResultsReady;
        public event Action RoundReset;

        public void EmitResults(float finishTime = 90f) => ResultsReady?.Invoke(finishTime);
        public void EmitReset() => RoundReset?.Invoke();
    }

    public static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("AFAREET_CAREER_RACE_SESSION_CONTRACT_OK");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run()
    {
        var definition = ChapterOneCareerEventContent.CreateDefinitions()[1];
        var source = new FakeRaceEventSource();
        var coordinator = new CareerRaceSessionCoordinator(source, definition);
        var callbacks = 0;
        coordinator.EvaluationReady += _ => callbacks++;

        Require(coordinator.RestartCount == 0, "New Career session must start at zero restarts.");
        Require(!coordinator.HasEvaluation, "New Career session must not expose stale evaluation.");

        source.EmitResults(90f);
        Require(coordinator.HasEvaluation, "Results must produce a Career evaluation.");
        Require(coordinator.LastEvaluation.AllCompleted, "Initial passing time-trial finish must satisfy finish + clean + time objectives.");
        Require(coordinator.LastEvaluation.CompletedCount == 3, "Initial passing finish must complete all three retained/evolved objectives.");
        Require(coordinator.LastEvaluation.Entries[2].ObjectiveId == $"time_{definition.Node.Id}", "Time-trial objective must remain third and mode-specific.");
        Require(coordinator.LastEvaluation.Entries[2].IsComplete, "Passing finish time must complete the time objective.");
        Require(callbacks == 1, "Initial results must publish exactly one evaluation callback.");

        source.EmitReset();
        Require(coordinator.RestartCount == 1, "Round reset must increment session restart count once.");
        Require(!coordinator.HasEvaluation && coordinator.LastEvaluation == null, "Round reset must invalidate prior evaluation.");

        source.EmitResults(90f);
        Require(coordinator.LastEvaluation.CompletedCount == 2, "Post-restart passing finish must complete finish + time only.");
        Require(!coordinator.LastEvaluation.AllCompleted, "Post-restart finish must fail clean objective.");
        Require(coordinator.LastEvaluation.Entries[0].ObjectiveId == $"finish_{definition.Node.Id}", "Evaluation order must retain finish objective first.");
        Require(coordinator.LastEvaluation.Entries[0].IsComplete, "Finish objective must remain complete after restart.");
        Require(coordinator.LastEvaluation.Entries[1].ObjectiveId == $"clean_{definition.Node.Id}", "Evaluation order must retain clean objective second.");
        Require(!coordinator.LastEvaluation.Entries[1].IsComplete, "Clean objective must fail after restart.");
        Require(coordinator.LastEvaluation.Entries[2].IsComplete, "Time objective must remain complete after restart when target time is met.");
        Require(callbacks == 2, "Second results event must publish exactly one additional evaluation callback.");

        coordinator.ResetSession();
        Require(coordinator.RestartCount == 0, "Explicit session reset must clear restart count.");
        Require(!coordinator.HasEvaluation, "Explicit session reset must clear stale evaluation.");
        source.EmitResults(90f);
        Require(coordinator.LastEvaluation.AllCompleted, "Fresh passing session must restore clean-run and time-trial semantics.");

        source.EmitReset();
        source.EmitResults(95f);
        Require(!coordinator.LastEvaluation.AllCompleted, "Slow finish must fail the time-trial objective.");
        Require(!coordinator.LastEvaluation.Entries[2].IsComplete, "Slow finish must keep time objective incomplete.");

        coordinator.Dispose();
        coordinator.Dispose();
        source.EmitReset();
        source.EmitResults();
        Require(coordinator.RestartCount == 1, "Disposed coordinator must ignore source events and preserve prior restart count.");
        Require(!coordinator.HasEvaluation, "Disposed coordinator must not capture new evaluations.");

        var threw = false;
        try
        {
            coordinator.ResetSession();
        }
        catch (ObjectDisposedException)
        {
            threw = true;
        }
        Require(threw, "ResetSession after dispose must fail closed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
