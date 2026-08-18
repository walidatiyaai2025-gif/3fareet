import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CAREER_DIR = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "CareerRuntime"
RACE_DIR = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Race"
PROGRESSION_DIR = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Progression"


def read(path: Path) -> str:
    assert path.is_file(), f"missing required file: {path}"
    return path.read_text(encoding="utf-8")


def test_career_runtime_assembly_direction_is_one_way():
    career = json.loads(read(CAREER_DIR / "Afareet.CareerRuntime.asmdef"))
    race = json.loads(read(RACE_DIR / "Afareet.Race.asmdef"))
    progression = json.loads(read(PROGRESSION_DIR / "Afareet.Progression.asmdef"))

    assert set(career["references"]) == {"Afareet.Race", "Afareet.Progression", "Afareet.GarageRuntime"}
    assert "Afareet.Progression" not in race.get("references", [])
    assert "Afareet.CareerRuntime" not in race.get("references", [])
    assert "Afareet.Race" not in progression.get("references", [])
    assert "Afareet.CareerRuntime" not in progression.get("references", [])


def test_coordinator_is_pure_and_uses_explicit_round_events_and_outcome_signal():
    source = read(CAREER_DIR / "CareerRaceSessionCoordinator.cs")

    for token in (
        "interface ICareerRaceEventSource",
        "event Action<float> ResultsReady",
        "event Action RoundReset",
        "interface ICareerRaceOutcomeMetricsSource",
        "bool FinishedSuccessfully",
        "metricsSource is ICareerRaceOutcomeMetricsSource",
        "CareerEventOutcome",
        "CareerObjectiveEvaluationPolicy.Evaluate",
        "restartCount = checked(restartCount + 1)",
        "lastEvaluation = null",
        "ResetSession()",
    ):
        assert token in source
    assert "UnityEngine" not in source
    assert "Afareet.Race" not in source
    assert "RaceRoundController" not in source
    assert "PlayerPrefs" not in source


def test_race_round_adapter_has_no_global_lookup_or_persistence_side_effects():
    source = read(CAREER_DIR / "RaceRoundCareerSessionAdapter.cs")

    for token in (
        "RaceDirectorCareerMetricsSource : ICareerRaceOutcomeMetricsSource",
        "public bool FinishedSuccessfully => !director.WasPlayerEliminated",
        "RaceRoundController",
        "round.ResultsReady += value",
        "round.ResultsReady -= value",
        "round.RoundReset += value",
        "round.RoundReset -= value",
        "CareerRaceSessionCoordinator",
    ):
        assert token in source
    for forbidden in (
        "FindObjectOfType",
        "FindFirstObjectByType",
        "FindAnyObjectByType",
        "GameObject.Find",
        "Resources.FindObjectsOfTypeAll",
        "PlayerPrefs",
        "CareerProgressionService",
        "SettlePlayerFinishReward",
    ):
        assert forbidden not in source


def test_game_session_navigation_browsing_remains_side_effect_free():
    source = read(CAREER_DIR / "CareerGameSession.cs")

    for token in (
        "CareerNavigationService navigationService",
        "public CareerNavigationSnapshot Navigation",
        "event Action<CareerNavigationSnapshot> NavigationChanged",
        "public CareerNavigationSnapshot SelectCareerNode",
        "public CareerNavigationSnapshot MoveCareerSelection",
        "navigationService.Select(Navigation, nodeId)",
        "navigationService.Move(Navigation, delta)",
        "RefreshNavigation(activeDefinition.Node.Id)",
        "RefreshNavigation(selectedNodeId)",
        "finished: !race.WasPlayerEliminated",
    ):
        assert token in source

    select_block = source.split("public CareerNavigationSnapshot SelectCareerNode", 1)[1].split("public CareerNavigationSnapshot MoveCareerSelection", 1)[0]
    move_block = source.split("public CareerNavigationSnapshot MoveCareerSelection", 1)[1].split("public bool TryActivateSelectedEvent", 1)[0]
    for block in (select_block, move_block):
        assert "activeDefinition =" not in block
        assert "ApplyChallengeConfiguration" not in block
        assert "ApplyBossVehicleConfiguration" not in block
        assert "ApplyTrack" not in block
        assert "RestartRace" not in block
        assert "StartRace" not in block


def test_selected_event_activation_is_explicit_safe_and_atomic():
    source = read(CAREER_DIR / "CareerGameSession.cs")
    for token in (
        "public bool TryActivateSelectedEvent()",
        "race.Phase == RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing",
        "selected.State == CareerNodeState.Locked",
        "FindDefinition(selected.Node.Id)",
        "var previousChallenge = race.ChallengeConfiguration",
        "var previousTrackId = trackRuntime.ActiveTrackId",
        "var previousBossVehicleId = bossVehicleRuntime.ActiveBossVehicleId",
        "var forceTrackRebuild = race.Phase == RaceRoundPhase.Results",
        "ApplyChallengeConfiguration(next)",
        "ApplyBossVehicleConfiguration(next)",
        "trackRuntime.ApplyTrack(next.Node.TrackId, forceTrackRebuild)",
        "forceTrackRebuild && race.Phase != RaceRoundPhase.Ready",
        "RestoreActivationRuntime(previousTrackId, previousChallenge, previousBossVehicleId)",
        "activeDefinition = next",
        "BindAdapter(activeDefinition)",
        "RefreshNavigation(activeDefinition.Node.Id)",
        "ActiveEventChanged?.Invoke(activeDefinition)",
    ):
        assert token in source

    activation = source.split("public bool TryActivateSelectedEvent()", 1)[1].split("public bool TryAdvanceToNextEvent()", 1)[0]
    guard = activation.index("RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing")
    locked = activation.index("selected.State == CareerNodeState.Locked")
    mutation = activation.index("ApplyChallengeConfiguration(next)")
    assignment = activation.index("activeDefinition = next")
    assert guard < mutation
    assert locked < mutation
    assert mutation < assignment
    assert "StartRace()" not in activation
    assert "RestartRace()" not in activation


def test_ai_racer_difficulty_hook_preserves_deterministic_base_profile():
    source = read(RACE_DIR / "AiRacer.cs")

    for token in (
        "private AiDifficultyTuning difficultyTuning = AiDifficultyTuning.Standard",
        "public AiDifficultyTuning DifficultyTuning => difficultyTuning",
        "public void ApplyDifficultyTuning(AiDifficultyTuning tuning)",
        "difficultyTuning = tuning",
        "skill * difficultyTuning.PaceMultiplier",
        "aggression * difficultyTuning.AggressionMultiplier",
        "ComputeAvoidance(effectiveAggression",
    ):
        assert token in source

    configure_block = source.split("public void Configure", 1)[1].split("public void ApplyDifficultyTuning", 1)[0]
    assert "new System.Random(17011 + rivalIndex * 7919)" in configure_block
    assert "difficultyTuning" not in configure_block
    assert "Random.Range" not in source


def test_elimination_domain_is_pure_deterministic_and_idempotent():
    source = read(RACE_DIR / "EliminationRaceRuntime.cs")
    assert "UnityEngine" not in source
    for token in (
        "sealed class EliminationDecision",
        "sealed class EliminationRaceRuntime",
        "EliminationGatePolicy.Build(checkpointCount, eliminationCount)",
        "processedGates.Contains(checkpointIndex)",
        "processedGates.Add(checkpointIndex)",
        "new HashSet<string>(StringComparer.Ordinal)",
        "active[active.Count - 1]",
        "eliminatedRacerIds.Add(eliminated)",
        "public void Reset()",
    ):
        assert token in source


def test_race_director_applies_challenge_roster_and_live_elimination_only_in_safe_paths():
    source = read(RACE_DIR / "RaceDirector.cs")

    for token in (
        "private RaceChallengeConfiguration challengeConfiguration = RaceChallengeConfiguration.Standard",
        "private bool challengeRosterDirty = true",
        "private EliminationRaceRuntime eliminationRuntime",
        "public RaceChallengeConfiguration ChallengeConfiguration => challengeConfiguration",
        "public int RequestedActiveRivalCount => challengeConfiguration.ActiveRivalCount",
        "public int ActiveRivalCount => CountActiveRivals()",
        "public bool WasPlayerEliminated => playerWasEliminated",
        "public void ApplyChallengeConfiguration(RaceChallengeConfiguration configuration)",
        "Phase == RaceRoundPhase.Countdown || Phase == RaceRoundPhase.Racing",
        "RebuildChallengeRoster()",
        "Math.Min(challengeConfiguration.ActiveRivalCount, registeredRivals.Count)",
        "rival.gameObject.SetActive(false)",
        "ai.ApplyDifficultyTuning(challengeConfiguration.AiDifficulty)",
        "ResetEliminationRuntime()",
        "new EliminationRaceRuntime(track.Waypoints.Count, racers.Count - 1)",
        "CheckpointAccepted += runtime.CheckpointAcceptedHandler",
        "CheckpointAccepted -= runtime.CheckpointAcceptedHandler",
        "eliminationRuntime.TryResolveGate",
        "runtime.Eliminated = true",
        "playerEliminationPosition = decision.FieldSizeBeforeElimination",
        "round.CompleteRoundExternally(eliminationTime)",
        "playerFinishRewardSnapshot = playerWasEliminated",
        "if (runtime.Eliminated) continue",
    ):
        assert token in source

    apply_block = source.split("public void ApplyChallengeConfiguration", 1)[1].split("public void StartRace", 1)[0]
    assert "throw new InvalidOperationException" in apply_block
    assert "challengeRosterDirty = true" in apply_block
    assert "RaceRoundPhase.Ready" in apply_block
    assert "Afareet.Progression" not in source
    assert "Afareet.CareerRuntime" not in source


def test_round_controller_supports_external_results_without_bypassing_flow_state():
    source = read(RACE_DIR / "RaceRoundController.cs")
    for token in (
        "public bool CompleteRoundExternally(float finishTime)",
        "if (flow.Phase != RaceRoundPhase.Racing)",
        "flow.Finish(finishTime)",
        "ResultsReady?.Invoke(finishTime)",
        "CompleteRoundExternally(finishTime)",
    ):
        assert token in source


def test_career_game_session_applies_node_balance_on_initial_bind_and_advance():
    source = read(CAREER_DIR / "CareerGameSession.cs")

    for token in (
        "public RaceChallengeConfiguration ActiveChallengeConfiguration",
        "ApplyChallengeConfiguration(activeDefinition)",
        "var previousChallenge = race.ChallengeConfiguration",
        "ApplyChallengeConfiguration(next)",
        "race.ApplyChallengeConfiguration(previousChallenge)",
        "CareerChallengeBalancePolicy.Resolve(definition.Node)",
    ):
        assert token in source

    helper = source.split("private void ApplyChallengeConfiguration", 1)[1].split("private void ApplyBossVehicleConfiguration", 1)[0]
    assert "race.ApplyChallengeConfiguration" in helper
    assert "CareerChallengeBalancePolicy.Resolve" in helper


def test_existing_race_and_progression_assemblies_remain_decoupled():
    race_source = read(RACE_DIR / "RaceDirector.cs")
    elimination_source = read(RACE_DIR / "EliminationRaceRuntime.cs")
    progression_source = read(PROGRESSION_DIR / "CareerObjectiveEvaluation.cs")

    assert "Afareet.Progression" not in race_source
    assert "Afareet.CareerRuntime" not in race_source
    assert "Afareet.Progression" not in elimination_source
    assert "Afareet.CareerRuntime" not in elimination_source
    assert "Afareet.Race" not in progression_source
    assert "Afareet.CareerRuntime" not in progression_source


def test_contract_projects_compile_authoritative_coordinator_and_elimination_sources():
    compile_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionCompile.csproj")
    runner_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionContractRunner.csproj")

    coordinator = "../../../unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerRaceSessionCoordinator.cs"
    elimination = "../../../unity_game/Assets/Afareet/Scripts/Race/EliminationRaceRuntime.cs"
    assert coordinator in compile_project
    assert coordinator in runner_project
    assert elimination in compile_project
    assert elimination in runner_project
    assert "RaceRoundCareerSessionAdapter.cs" not in compile_project
    assert "RaceRoundCareerSessionAdapter.cs" not in runner_project


def main():
    tests = sorted(
        (name, value)
        for name, value in globals().items()
        if name.startswith("test_") and callable(value)
    )
    for name, test in tests:
        test()
        print(f"PASS {name}")
    print(f"AFAREET_CAREER_RACE_SOURCE_CONTRACT_OK tests={len(tests)}")


if __name__ == "__main__":
    main()
