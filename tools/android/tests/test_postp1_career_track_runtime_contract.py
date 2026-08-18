import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORLD = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "World"
CAREER = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "CareerRuntime"
CORE = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Core"


def read(path: Path) -> str:
    assert path.is_file(), f"missing required file: {path}"
    return path.read_text(encoding="utf-8")


def test_catalog_is_pure_exact_and_fail_closed():
    source = read(WORLD / "CairoCareerTrackCatalog.cs")
    assert "UnityEngine" not in source
    for token in (
        'CornicheNightId = "cairo_corniche_night"',
        'KhanSprintId = "khan_el_khalili_sprint"',
        'RingRoadMidnightId = "ring_road_midnight"',
        'CitadelDriftId = "citadel_drift"',
        'PyramidsSpiritRunId = "pyramids_spirit_run"',
        "new CairoCareerTrackSpec(CornicheNightId, 1f, 0f)",
        "CultureInfo.InvariantCulture",
        "Unknown Cairo Career track id",
        "StringComparer.Ordinal.Equals",
    ):
        assert token in source


def test_builder_reuses_authoritative_p1_builder_without_external_assets():
    source = read(WORLD / "CairoCareerTrackBuilder.cs")
    for token in (
        "CairoCareerTrackCatalog.Resolve(trackId)",
        "root.transform.localScale = Vector3.one * spec.UniformScale",
        "Quaternion.Euler(0f, spec.YawDegrees, 0f)",
        "CairoTrackBuilder.Build(root.transform)",
        "track.Waypoints.Count < 2",
        "DestroyCreatedRoot(root)",
    ):
        assert token in source
    for forbidden in ("AssetDatabase", "Resources.Load", "Addressables", "BossVehicleId"):
        assert forbidden not in source


def test_runtime_controller_swaps_only_in_safe_phase_and_rebinds_ai():
    source = read(CORE / "CareerTrackRuntimeController.cs")
    for token in (
        "CareerTrackRuntimeController : ICareerTrackRuntime",
        "CairoCareerTrackCatalog.Resolve(trackId)",
        "RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing",
        "CairoCareerTrackBuilder.Build(host, spec)",
        "race.Configure(player, build.Track)",
        "RebindAiPaths(build.Track)",
        "ai.Configure(track.Waypoints, index)",
        "previousRoot.SetActive(false)",
        "ActiveTrackId = spec.Id",
    ):
        assert token in source


def test_career_session_applies_track_on_bind_and_advance():
    source = read(CAREER / "CareerGameSession.cs")
    for token in (
        "private ICareerTrackRuntime trackRuntime",
        "public string ActiveTrackId => trackRuntime?.ActiveTrackId",
        "trackRuntime.ApplyTrack(activeDefinition.Node.TrackId)",
        "var previousTrackId = trackRuntime.ActiveTrackId",
        "var trackChanged = trackRuntime.ApplyTrack(next.Node.TrackId)",
        "StartFreshRaceAfterTrackChange()",
        "trackRuntime.ApplyTrack(previousTrackId)",
    ):
        assert token in source


def test_production_bootstrap_uses_live_controller_not_passive_runtime():
    source = read(CORE / "AfareetBootstrap.cs")
    for token in (
        "CairoCareerTrackBuilder.Build(transform, CairoCareerTrackCatalog.CornicheNightId)",
        "new CareerTrackRuntimeController(",
        "trackBuild.Root",
        "CairoCareerTrackCatalog.CornicheNightId",
        "career.Configure(",
        "careerTracks,",
        "careerBossVehicles);",
    ):
        assert token in source
    assert "new PassiveCareerTrackRuntime" not in source


def test_assembly_direction_remains_without_world_dependency_in_career_runtime():
    career = json.loads(read(CAREER / "Afareet.CareerRuntime.asmdef"))
    assert set(career["references"]) == {"Afareet.Race", "Afareet.Progression", "Afareet.GarageRuntime"}
    assert "Afareet.World" not in career["references"]


def test_contract_and_metadata_are_tracked():
    compile_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionCompile.csproj")
    runner_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionContractRunner.csproj")
    for project in (compile_project, runner_project):
        assert "CairoCareerTrackCatalog.cs" in project
        assert "CareerTrackRuntime.cs" in project

    for path in (
        WORLD / "CairoCareerTrackCatalog.cs.meta",
        WORLD / "CairoCareerTrackBuilder.cs.meta",
        CAREER / "CareerTrackRuntime.cs.meta",
        CORE / "CareerTrackRuntimeController.cs.meta",
    ):
        meta = read(path)
        assert "fileFormatVersion: 2" in meta
        assert "guid:" in meta


def main():
    tests = sorted(
        (name, value)
        for name, value in globals().items()
        if name.startswith("test_") and callable(value)
    )
    for name, test in tests:
        test()
        print(f"PASS {name}")
    print(f"AFAREET_CAREER_TRACK_RUNTIME_SOURCE_CONTRACT_OK tests={len(tests)}")


if __name__ == "__main__":
    main()
