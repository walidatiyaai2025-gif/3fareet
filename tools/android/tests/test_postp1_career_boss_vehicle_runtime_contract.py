from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CAREER = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "CareerRuntime"
CORE = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Core"
VEHICLE = ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Vehicle"


def read(path: Path) -> str:
    assert path.is_file(), f"missing required file: {path}"
    return path.read_text(encoding="utf-8")


def test_boss_runtime_seam_is_pure_and_fail_closed():
    source = read(CAREER / "CareerBossVehicleRuntime.cs")
    assert "UnityEngine" not in source
    for token in (
        "interface ICareerBossVehicleRuntime",
        "string ActiveBossVehicleId",
        "ApplyBossVehicle(string bossVehicleId)",
        "ClearBossVehicle()",
        "PassiveCareerBossVehicleRuntime",
        "string.IsNullOrWhiteSpace(bossVehicleId)",
        "StringComparer.Ordinal.Equals",
    ):
        assert token in source


def test_career_game_session_wires_boss_on_resume_advance_and_rollback():
    source = read(CAREER / "CareerGameSession.cs")
    for token in (
        "private ICareerBossVehicleRuntime bossVehicleRuntime",
        "public string ActiveBossVehicleId => bossVehicleRuntime?.ActiveBossVehicleId",
        "new PassiveCareerBossVehicleRuntime()",
        "bossVehicleRuntime = careerBossVehicleRuntime",
        "ApplyBossVehicleConfiguration(activeDefinition)",
        "var previousBossVehicleId = bossVehicleRuntime.ActiveBossVehicleId",
        "ApplyBossVehicleConfiguration(next)",
        "RestoreBossVehicle(previousBossVehicleId)",
        "bossVehicleRuntime.ApplyBossVehicle(definition.Node.BossVehicleId)",
        "bossVehicleRuntime.ClearBossVehicle()",
    ):
        assert token in source

    helper = source.split("private void ApplyBossVehicleConfiguration", 1)[1].split("private void RestoreBossVehicle", 1)[0]
    assert "definition.Node.BossVehicleId" in helper
    assert "ClearBossVehicle" in helper
    assert "ApplyBossVehicle" in helper


def test_production_bootstrap_installs_real_boss_runtime_and_shared_catalog():
    source = read(CORE / "AfareetBootstrap.cs")
    for token in (
        "var garageCatalog = GarageCatalog.CreateDefault()",
        "new CareerBossVehicleRuntimeController(",
        "garageCatalog,",
        "careerBossVehicles);",
        "garage.ConfigureWithPlayerPrefs(career, garageCatalog)",
    ):
        assert token in source
    assert "new PassiveCareerBossVehicleRuntime" not in source


def test_production_boss_controller_uses_authoritative_catalog_stats_and_asset_ledger_fallback():
    source = read(CORE / "CareerBossVehicleRuntimeController.cs")
    for token in (
        "CareerBossVehicleRuntimeController : ICareerBossVehicleRuntime",
        "catalog.GetRequired(bossVehicleId)",
        "catalog.NormalizeStats(bossVehicleId)",
        "SetVehiclePerformanceProfile",
        "ResetVehiclePerformanceProfile",
        "Resources.Load<GameObject>(definition.PreviewResourcePath)",
        'MissingProductionAssetRequest = "EXT-ASSET-002"',
        "RaceRoundPhase.Countdown || race.Phase == RaceRoundPhase.Racing",
    ):
        assert token in source

    ledger = read(ROOT / "EXTERNAL_ASSET_REQUESTS.txt")
    assert "EXT-ASSET-002" in ledger
    assert "Rival_03_CompactPrototype_Production" in ledger


def test_vehicle_performance_remains_separate_from_powerup_modifier_layer():
    source = read(VEHICLE / "ArcadeCarController.cs")
    for token in (
        "private ArcadeDriveModifier externalDriveModifier",
        "private VehiclePerformanceProfile vehiclePerformanceProfile",
        "public VehiclePerformanceProfile VehiclePerformanceProfile",
        "SetVehiclePerformanceProfile",
        "ResetVehiclePerformanceProfile",
        "ResetExternalDriveModifier",
    ):
        assert token in source
    assert "ResetExternalDriveModifier()\n        {\n            externalDriveModifier = ArcadeDriveModifier.Neutral();" in source


def test_pure_contract_projects_compile_boss_runtime_seam():
    compile_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionCompile.csproj")
    runner_project = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionContractRunner.csproj")
    runner = read(ROOT / "tools" / "android" / "contracts" / "CareerRaceSessionContractRunner.cs")
    for project in (compile_project, runner_project):
        assert "CareerBossVehicleRuntime.cs" in project
    assert "RunBossVehicleRuntimeContract();" in runner
    assert 'bossNode.BossVehicleId == "djinn_spirit"' in runner


def main():
    tests = sorted(
        (name, value)
        for name, value in globals().items()
        if name.startswith("test_") and callable(value)
    )
    for name, test in tests:
        test()
        print(f"PASS {name}")
    print(f"AFAREET_CAREER_BOSS_VEHICLE_RUNTIME_SOURCE_CONTRACT_OK tests={len(tests)}")


if __name__ == "__main__":
    main()
