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

    assert set(career["references"]) == {"Afareet.Race", "Afareet.Progression"}
    assert "Afareet.Progression" not in race.get("references", [])
    assert "Afareet.CareerRuntime" not in race.get("references", [])
    assert "Afareet.Race" not in progression.get("references", [])
    assert "Afareet.CareerRuntime" not in progression.get("references", [])


def test_coordinator_is_pure_and_uses_explicit_round_events():
    source = read(CAREER_DIR / "CareerRaceSessionCoordinator.cs")

    assert "interface ICareerRaceEventSource" in source
    assert "event Action<float> ResultsReady" in source
    assert "event Action RoundReset" in source
    assert "CareerEventOutcome" in source
    assert "CareerObjectiveEvaluationPolicy.Evaluate" in source
    assert "restartCount = checked(restartCount + 1)" in source
    assert "lastEvaluation = null" in source
    assert "ResetSession()" in source
    assert "UnityEngine" not in source
    assert "Afareet.Race" not in source
    assert "RaceRoundController" not in source
    assert "PlayerPrefs" not in source


def test_race_round_adapter_has_no_global_lookup_or_persistence_side_effects():
    source = read(CAREER_DIR / "RaceRoundCareerSessionAdapter.cs")

    assert "RaceRoundController" in source
    assert "round.ResultsReady += value" in source
    assert "round.ResultsReady -= value" in source
    assert "round.RoundReset += value" in source
    assert "round.RoundReset -= value" in source
    assert "CareerRaceSessionCoordinator" in source
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


def test_game_session_exposes_navigation_without_mutating_active_event_on_selection():
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
    ):
        assert token in source

    select_block = source.split("public CareerNavigationSnapshot SelectCareerNode", 1)[1].split("public CareerNavigationSnapshot MoveCareerSelection", 1)[0]
    move_block = source.split("public CareerNavigationSnapshot MoveCareerSelection", 1)[1].split("public bool TryAdvanceToNextEvent", 1)[0]
    for block in (select_block, move_block):
        assert "activeDefinition =" not in block
        assert "RestartRace" not in block
        assert "StartRace" not in block


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


def test_race_director_applies_challenge_roster_only_in_safe_phases():
    source = read(RACE_DIR / "RaceDirector.cs")

    for token in (
        "private RaceChallengeConfiguration challengeConfiguration = RaceChallengeConfiguration.Standard",
        "private bool challengeRosterDirty = true",
        "public RaceChallengeConfiguration ChallengeConfiguration => challengeConfiguration",
        "public int RequestedActiveRivalCount => challengeConfiguration.ActiveRivalCount",
        "public int ActiveRivalCount => Math.Max(0, racers.Count - 1)",
        "public void ApplyChallengeConfiguration(RaceChallengeConfiguration configuration)",
        "Phase == RaceRoundPhase.Countdown || Phase == RaceRoundPhase.Racing",
        "RebuildChallengeRoster()",
        "Math.Min(challengeConfiguration.ActiveRivalCount, registeredRivals.Count)",
        "rival.gameObject.SetActive(false)",
        "ai.ApplyDifficultyTuning(challengeConfiguration.AiDifficulty)",
    ):
        assert token in source

    apply_block = source.split("public void ApplyChallengeConfiguration", 1)[1].split("public void StartRace", 1)[0]
    assert "throw new InvalidOperationException" in apply_block
    assert "challengeRosterDirty = true" in apply_block
    assert "RaceRoundPhase.Ready" in apply_block
    assert "Afareet.Progression" not in source
    assert "Afareet.CareerRuntime" not in source


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

    helper = source.split("private void ApplyChallengeConfiguration", 1)[1].split("private void BindAdapter", 1)[0]
    assert "race.ApplyChallengeConfiguration" in helper
    assert "CareerChallengeBalancePolicy.Resolve" in helper


def test_existing_race_and_progression_assemblies_remain_decoupled():
    race_source = read(RACE_DIR / "RaceDirector.cs")
    progression_source = read(PROGRESSION_DIR / "CareerObjectiveEvaluation.cs")

    assert "Afareet.Progression" not in race_source
    assert "Afareet.CareerRuntime" not in race_source
    assert "Afareet.Race" not in progression_source
    assert "Afareet.CareerRuntime" not in progression_source


def test_contract_projects_compile_authoritative_coordinator_source():
    compile_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionCompile.csproj")
    runner_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionContractRunner.csproj")

    expected = "../../../unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerRaceSessionCoordinator.cs"
    assert expected in compile_project
    assert expected in runner_project
    assert "RaceRoundCareerSessionAdapter.cs" not in compile_project
    assert "RaceRoundCareerSessionAdapter.cs" not in runner_project
