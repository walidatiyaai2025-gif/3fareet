using System;
using System.Collections.Generic;
using Afareet.CareerRuntime;
using Afareet.Progression;
using Afareet.Race;
using Afareet.World;

internal static class CareerRaceSessionContractRunner
{
    private sealed class FakeRaceEventSource : ICareerRaceEventSource
    {
        public event Action<float> ResultsReady;
        public event Action RoundReset;

        public void EmitResults(float finishTime = 90f) => ResultsReady?.Invoke(finishTime);
        public void EmitReset() => RoundReset?.Invoke();
    }

    private sealed class FakeOutcomeMetrics : ICareerRaceOutcomeMetricsSource
    {
        public int FinalPosition { get; set; } = 1;
        public int DriftScore { get; set; }
        public bool FinishedSuccessfully { get; set; } = true;
    }

    public static int Main()
    {
        try
        {
            Run();
            RunFailedOutcomeContract();
            RunChallengeBalanceContract();
            RunEliminationRuntimeContract();
            RunTrackCatalogContract();
            RunBossVehicleRuntimeContract();
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

    private static void RunFailedOutcomeContract()
    {
        var definition = ChapterOneCareerEventContent.CreateDefinitions()[2];
        var source = new FakeRaceEventSource();
        var metrics = new FakeOutcomeMetrics
        {
            FinalPosition = 4,
            FinishedSuccessfully = false
        };
        using var coordinator = new CareerRaceSessionCoordinator(source, definition, metrics);

        source.EmitResults(60f);
        Require(coordinator.HasEvaluation, "Elimination loss must still produce a results evaluation.");
        Require(coordinator.LastEvaluation.CompletedCount == 0,
            "Player elimination must fail finish, clean and first-place objectives even though the round reached Results.");
        Require(!coordinator.LastEvaluation.AllCompleted,
            "Elimination loss must never complete the Career node.");
    }

    private static void RunChallengeBalanceContract()
    {
        var nodes = ChapterOneCareerContent.CreateFoundation().Nodes;
        Require(nodes.Count == 5, "Chapter 1 challenge balance contract requires the retained five-node order.");

        RequireChallenge(CareerChallengeBalancePolicy.Resolve(nodes[0]), 3, .88f, .90f, false, "Circuit");
        RequireChallenge(CareerChallengeBalancePolicy.Resolve(nodes[1]), 0, 1f, 1f, false, "Time Trial");
        RequireChallenge(CareerChallengeBalancePolicy.Resolve(nodes[2]), 3, .98f, 1.04f, true, "Elimination");
        RequireChallenge(CareerChallengeBalancePolicy.Resolve(nodes[3]), 0, 1f, 1f, false, "Drift");
        RequireChallenge(CareerChallengeBalancePolicy.Resolve(nodes[4]), 1, 1.08f, 1.14f, false, "Boss");
        RequireChallenge(RaceChallengeConfiguration.Standard, 3, 1f, 1f, false, "Standard P1");
    }

    private static void RunEliminationRuntimeContract()
    {
        var runtime = new EliminationRaceRuntime(checkpointCount: 4, eliminationCount: 3);
        Require(runtime.Gates.Count == 3, "Four-checkpoint elimination must expose three gates.");
        Require(runtime.Gates[0] == 1 && runtime.Gates[1] == 2 && runtime.Gates[2] == 3,
            "Chapter 1 elimination gates must remain checkpoints 1, 2 and 3.");

        Require(!runtime.TryResolveGate(0, new[] { "PLAYER", "R1", "R2", "R3" }, out _),
            "Start/finish checkpoint must not eliminate a racer.");

        Require(runtime.TryResolveGate(1, new[] { "PLAYER", "R1", "R2", "R3" }, out var first),
            "First elimination gate must resolve exactly once.");
        Require(first.EliminatedRacerId == "R3" && first.FieldSizeBeforeElimination == 4 && first.RemainingRacerCount == 3,
            "First gate must eliminate the current last-place active racer.");
        Require(runtime.IsEliminated("R3"), "Eliminated racer must be retained in deterministic state.");
        Require(!runtime.TryResolveGate(1, new[] { "R1", "PLAYER", "R2" }, out _),
            "Duplicate callbacks for an already-processed gate must be ignored.");

        Require(runtime.TryResolveGate(2, new[] { "R1", "PLAYER", "R2" }, out var second),
            "Second gate must resolve after the active ranking changes.");
        Require(second.EliminatedRacerId == "R2", "Second gate must eliminate the new last-place racer.");
        Require(runtime.TryResolveGate(3, new[] { "PLAYER", "R1" }, out var third),
            "Final gate must collapse the field to one survivor.");
        Require(third.EliminatedRacerId == "R1" && third.RemainingRacerCount == 1,
            "Final gate must leave exactly one survivor.");
        Require(runtime.EliminatedRacerCount == 3 && runtime.ProcessedGateCount == 3,
            "All configured eliminations must be accounted for once.");

        runtime.Reset();
        Require(runtime.EliminatedRacerCount == 0 && runtime.ProcessedGateCount == 0,
            "Restart/reset must clear elimination state completely.");
        Require(runtime.TryResolveGate(1, new[] { "R1", "R2", "PLAYER" }, out var playerLoss),
            "Player can be selected by the same deterministic last-place rule.");
        Require(playerLoss.EliminatedRacerId == "PLAYER" && playerLoss.FieldSizeBeforeElimination == 3,
            "Player-last gate must produce an explicit player elimination decision.");

        RequireThrows<ArgumentException>(
            () => runtime.TryResolveGate(2, new[] { "R1", "R1" }, out _),
            "Duplicate ranking ids must fail closed.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => runtime.TryResolveGate(4, new[] { "R1", "R2" }, out _),
            "Out-of-range checkpoint ids must fail closed.");
    }

    private static void RunTrackCatalogContract()
    {
        var nodes = ChapterOneCareerContent.CreateFoundation().Nodes;
        var expectedIds = new[]
        {
            CairoCareerTrackCatalog.CornicheNightId,
            CairoCareerTrackCatalog.KhanSprintId,
            CairoCareerTrackCatalog.RingRoadMidnightId,
            CairoCareerTrackCatalog.CitadelDriftId,
            CairoCareerTrackCatalog.PyramidsSpiritRunId
        };

        Require(CairoCareerTrackCatalog.Specs.Count == expectedIds.Length,
            "Career TrackId catalog must expose exactly the retained five Chapter 1 specs.");
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < expectedIds.Length; index++)
        {
            Require(nodes[index].TrackId == expectedIds[index],
                $"Chapter 1 TrackId order drifted at index {index}.");
            var spec = CairoCareerTrackCatalog.Resolve(expectedIds[index]);
            Require(StringComparer.Ordinal.Equals(spec.Id, expectedIds[index]),
                $"Resolved TrackId drifted at index {index}.");
            Require(signatures.Add(spec.DeterministicSignature),
                $"Career TrackId spec signature must be distinct: {spec.Id}.");
        }

        var standard = CairoCareerTrackCatalog.Resolve(CairoCareerTrackCatalog.CornicheNightId);
        Require(Math.Abs(standard.UniformScale - 1f) < .0001f && Math.Abs(standard.YawDegrees) < .0001f,
            "Corniche TrackId must preserve the existing P1 authored route transform exactly.");
        RequireThrows<ArgumentException>(
            () => CairoCareerTrackCatalog.Resolve("missing_track"),
            "Unknown Career TrackId must fail closed.");

        var passive = new PassiveCareerTrackRuntime();
        Require(!passive.ApplyTrack(CairoCareerTrackCatalog.CornicheNightId),
            "Compatibility TrackId runtime must never claim it rebuilt live race geometry.");
        Require(passive.ActiveTrackId == CairoCareerTrackCatalog.CornicheNightId,
            "Compatibility TrackId runtime must still retain the selected stable id.");
    }

    private static void RunBossVehicleRuntimeContract()
    {
        var bossNode = ChapterOneCareerContent.CreateFoundation().Nodes[4];
        Require(bossNode.Mode == CareerRaceMode.Boss, "Retained fifth Chapter 1 node must remain Boss mode.");
        Require(bossNode.BossVehicleId == "djinn_spirit", "Retained BossVehicleId must remain djinn_spirit.");

        var passive = new PassiveCareerBossVehicleRuntime();
        Require(passive.ActiveBossVehicleId == null, "Passive boss runtime must start clear.");
        Require(!passive.ApplyBossVehicle(bossNode.BossVehicleId),
            "Compatibility boss runtime must never claim it mutated live Unity state.");
        Require(passive.ActiveBossVehicleId == "djinn_spirit",
            "Passive boss runtime must retain the stable BossVehicleId for contract compatibility.");
        Require(!passive.ApplyBossVehicle("djinn_spirit"),
            "Repeated BossVehicleId apply must be idempotent.");
        Require(!passive.ClearBossVehicle(),
            "Compatibility boss runtime clear must not claim live mutation.");
        Require(passive.ActiveBossVehicleId == null,
            "Boss runtime clear must remove the active stable ID.");
        Require(!passive.ClearBossVehicle(), "Repeated boss clear must remain idempotent.");
        RequireThrows<ArgumentException>(
            () => passive.ApplyBossVehicle(" "),
            "Blank BossVehicleId must fail closed.");
    }

    private static void RequireChallenge(
        RaceChallengeConfiguration configuration,
        int activeRivals,
        float pace,
        float aggression,
        bool elimination,
        string label)
    {
        Require(configuration.ActiveRivalCount == activeRivals, $"{label} active-rival count drifted.");
        Require(Math.Abs(configuration.AiDifficulty.PaceMultiplier - pace) < .0001f, $"{label} AI pace drifted.");
        Require(Math.Abs(configuration.AiDifficulty.AggressionMultiplier - aggression) < .0001f, $"{label} AI aggression drifted.");
        Require(configuration.EliminationEnabled == elimination, $"{label} elimination flag drifted.");
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        var threw = false;
        try
        {
            action();
        }
        catch (TException)
        {
            threw = true;
        }
        Require(threw, message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
