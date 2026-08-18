using System;
using System.Collections.Generic;
using Afareet.Progression;

internal static class CareerFoundationContractRunner
{
    private static int Main()
    {
        try
        {
            ChapterOneParityContract();
            ValidationContract();
            OrderingAndStateContract();
            NavigationContract();
            Console.WriteLine("Career foundation behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Career foundation behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void ChapterOneParityContract()
    {
        var chapter = ChapterOneCareerContent.CreateFoundation();
        CareerDefinitionPolicy.ValidateChapterOrThrow(chapter);

        Require(chapter.Id == "chapter_01_cairo_after_dark", "chapter id drifted");
        Require(chapter.Title == "Cairo After Dark", "chapter title drifted");
        Require(chapter.Order == 1, "chapter order drifted");
        Require(chapter.Nodes.Count == 5, "Chapter 1 must contain exactly five retained nodes");

        RequireNode(chapter.Nodes[0], "c01_r01", CareerRaceMode.Circuit, "cairo_corniche_night", 0);
        RequireNode(chapter.Nodes[1], "c01_r02", CareerRaceMode.TimeTrial, "khan_el_khalili_sprint", 2);
        Require(chapter.Nodes[1].TargetTimeSeconds == 92d, "Clock of Khan target must remain 92 seconds");
        RequireNode(chapter.Nodes[2], "c01_r03", CareerRaceMode.Elimination, "ring_road_midnight", 4);
        RequireNode(chapter.Nodes[3], "c01_r04", CareerRaceMode.DriftChallenge, "citadel_drift", 6);
        Require(chapter.Nodes[3].TargetDriftScore == 12000, "Spirit Drift target must remain 12000");
        RequireNode(chapter.Nodes[4], "c01_boss", CareerRaceMode.Boss, "pyramids_spirit_run", 9);
        Require(chapter.Nodes[4].BossVehicleId == "djinn_spirit", "boss vehicle id drifted");
    }

    private static void ValidationContract()
    {
        RequireThrows(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
            new CareerRaceNode("tt", "TT", CareerRaceMode.TimeTrial, "track", 0)),
            "time trial without target must fail closed");
        RequireThrows(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
            new CareerRaceNode("drift", "Drift", CareerRaceMode.DriftChallenge, "track", 0)),
            "drift challenge without target must fail closed");
        RequireThrows(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
            new CareerRaceNode("boss", "Boss", CareerRaceMode.Boss, "track", 0)),
            "boss without vehicle must fail closed");
        RequireThrows(() => CareerDefinitionPolicy.ValidateNodeOrThrow(
            new CareerRaceNode("nan", "NaN", CareerRaceMode.TimeTrial, "track", 0, double.NaN)),
            "non-finite time target must fail closed");

        var duplicateNodes = new CareerChapter(
            "chapter",
            "Chapter",
            1,
            new[]
            {
                new CareerRaceNode("same", "One", CareerRaceMode.Circuit, "track", 0),
                new CareerRaceNode("same", "Two", CareerRaceMode.Elimination, "track", 0)
            });
        RequireThrows(() => CareerDefinitionPolicy.ValidateChapterOrThrow(duplicateNodes),
            "duplicate node ids must fail closed");

        RequireThrows(() => new CareerMap(new[]
        {
            SimpleChapter("duplicate", 1),
            SimpleChapter("duplicate", 2)
        }), "duplicate chapter ids must fail closed");
    }

    private static void OrderingAndStateContract()
    {
        var map = new CareerMap(new[]
        {
            SimpleChapter("chapter_b", 2),
            SimpleChapter("chapter_c", 1),
            SimpleChapter("chapter_a", 1)
        });
        Require(map.Chapters[0].Id == "chapter_a", "same-order chapters must use deterministic ordinal tie-break");
        Require(map.Chapters[1].Id == "chapter_c", "same-order chapters must remain deterministic");
        Require(map.Chapters[2].Id == "chapter_b", "higher order chapter must sort later");

        var chapterOne = ChapterOneCareerContent.CreateFoundation();
        var stateMap = new CareerMap(new[] { chapterOne });
        var drift = chapterOne.Nodes[3];
        Require(stateMap.NodeState(drift, 5, new HashSet<string>()) == CareerNodeState.Locked,
            "node below star gate must be locked");
        Require(stateMap.NodeState(drift, 6, new HashSet<string>()) == CareerNodeState.Available,
            "node at star gate must be available");
        Require(stateMap.NodeState(drift, 0, new HashSet<string>(StringComparer.Ordinal) { drift.Id }) == CareerNodeState.Completed,
            "completed state must win over star gate");
    }

    private static void NavigationContract()
    {
        var service = new CareerNavigationService();
        var chapter = ChapterOneCareerContent.CreateFoundation();
        var map = new CareerMap(new[] { chapter });
        var initial = service.Build(map, CareerProgress.Empty());

        Require(initial.Nodes.Count == 5, "navigation must flatten all Chapter 1 nodes");
        Require(initial.SelectedNodeId == "c01_r01", "navigation must select first available node by default");
        Require(initial.Nodes[0].State == CareerNodeState.Available, "first Chapter 1 node must be available at zero stars");
        Require(initial.Nodes[1].State == CareerNodeState.Locked, "second Chapter 1 node must respect its star gate");
        Require(initial.Nodes[0].ChapterIndex == 0 && initial.Nodes[0].NodeIndex == 0 && initial.Nodes[0].FlatIndex == 0,
            "navigation indices must be deterministic");

        var progressed = new CareerProgress(
            CareerProgress.CurrentVersion,
            6,
            new[] { "c01_r01" },
            Array.Empty<string>());
        var selected = service.Build(map, progressed, "c01_r03");
        Require(selected.SelectedNodeId == "c01_r03", "preferred stable node id must control selection");
        Require(selected.Nodes[0].State == CareerNodeState.Completed, "completed node must remain completed");
        Require(selected.Nodes[1].State == CareerNodeState.Available, "star-unlocked node must be available");
        Require(selected.Nodes[3].State == CareerNodeState.Available, "node at exact star gate must be available");
        Require(selected.Nodes[4].State == CareerNodeState.Locked, "boss must remain locked below nine stars");

        var wrappedBackward = service.Move(selected, -3);
        Require(wrappedBackward.SelectedNodeId == "c01_boss", "negative navigation must wrap deterministically");
        var wrappedForward = service.Move(wrappedBackward, 1);
        Require(wrappedForward.SelectedNodeId == "c01_r01", "positive navigation must wrap deterministically");
        var explicitSelection = service.Select(wrappedForward, "c01_r04");
        Require(explicitSelection.SelectedNodeId == "c01_r04", "explicit selection must use stable node id");
        RequireThrows(() => service.Select(explicitSelection, "missing"), "unknown selected node id must fail closed");
        RequireThrows(() => service.Build(map, progressed, "missing"), "unknown preferred node id must fail closed");

        var gatedChapter = new CareerChapter(
            "gated",
            "Gated",
            1,
            new[] { new CareerRaceNode("gated_race", "Race", CareerRaceMode.Circuit, "track", 0) },
            requiredStars: 5);
        var gatedMap = new CareerMap(new[] { gatedChapter });
        var belowChapterGate = new CareerProgress(
            CareerProgress.CurrentVersion,
            4,
            Array.Empty<string>(),
            Array.Empty<string>());
        Require(service.Build(gatedMap, belowChapterGate).Nodes[0].State == CareerNodeState.Locked,
            "chapter star gate must lock otherwise-open node");
        var completedBelowGate = new CareerProgress(
            CareerProgress.CurrentVersion,
            0,
            new[] { "gated_race" },
            Array.Empty<string>());
        Require(service.Build(gatedMap, completedBelowGate).Nodes[0].State == CareerNodeState.Completed,
            "completed node must remain completed even below a later chapter gate");

        var duplicateMap = new CareerMap(new[]
        {
            new CareerChapter(
                "one",
                "One",
                1,
                new[] { new CareerRaceNode("shared", "Shared A", CareerRaceMode.Circuit, "a", 0) }),
            new CareerChapter(
                "two",
                "Two",
                2,
                new[] { new CareerRaceNode("shared", "Shared B", CareerRaceMode.Circuit, "b", 0) })
        });
        RequireThrows(() => service.Build(duplicateMap, CareerProgress.Empty()),
            "duplicate node ids across chapters must fail closed for navigation");
    }

    private static CareerChapter SimpleChapter(string id, int order)
    {
        return new CareerChapter(
            id,
            id,
            order,
            new[] { new CareerRaceNode(id + "_race", "Race", CareerRaceMode.Circuit, "track", 0) });
    }

    private static void RequireNode(CareerRaceNode node, string id, CareerRaceMode mode, string trackId, int requiredStars)
    {
        Require(node.Id == id, $"node id drifted: expected {id}");
        Require(node.Mode == mode, $"node mode drifted: {id}");
        Require(node.TrackId == trackId, $"track id drifted: {id}");
        Require(node.RequiredStars == requiredStars, $"star gate drifted: {id}");
    }

    private static void RequireThrows(Action action, string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (ArgumentException)
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