import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
GARAGE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/GarageRuntime"
CAREER_BRIDGE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerGarageBridge.cs"
CAREER_SESSION = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerGarageSession.cs"
CAREER_ASMDEF = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/CareerRuntime/Afareet.CareerRuntime.asmdef"
BOOTSTRAP = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Core/AfareetBootstrap.cs"
COMPILE_PROJECT = REPO_ROOT / "tools/android/contracts/GarageRuntimeCompile.csproj"
RUNNER_PROJECT = REPO_ROOT / "tools/android/contracts/GarageRuntimeContractRunner.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/GarageRuntimeContractRunner.cs"
WORKFLOW = REPO_ROOT / ".github/workflows/postp1-garage-runtime-contract.yml"


class PostP1GarageRuntimeContractTests(unittest.TestCase):
    def test_garage_runtime_module_covers_catalog_state_service_and_persistence(self):
        required_files = (
            "Afareet.GarageRuntime.asmdef",
            "GarageCatalog.cs",
            "GarageState.cs",
            "GarageService.cs",
            "GaragePersistence.cs",
            "PlayerPrefsGarageStateStorage.cs",
            "GaragePreviewResolver.cs",
        )
        for name in required_files:
            self.assertTrue((GARAGE / name).is_file(), name)

    def test_catalog_has_versioned_schema_four_archetypes_and_stable_ids(self):
        source = (GARAGE / "GarageCatalog.cs").read_text(encoding="utf-8")
        for required in (
            "CurrentSchemaVersion = 1",
            'StarterVehicleId = "afareet_king"',
            '"wedge_coupe"',
            '"fastback_muscle"',
            '"djinn_spirit"',
            "GarageVehicleArchetype.Hero",
            "GarageVehicleArchetype.WedgeCoupe",
            "GarageVehicleArchetype.FastbackMuscle",
            "GarageVehicleArchetype.CompactPrototype",
            "NormalizeStats",
            "duplicate vehicle id",
            "GarageCosmeticSet",
            "BuildUniqueOptions",
        ):
            self.assertIn(required, source)

    def test_service_enforces_unlock_equip_and_cosmetic_rules(self):
        source = (GARAGE / "GarageService.cs").read_text(encoding="utf-8")
        for required in (
            "ListVehicles",
            "GetDetail",
            "Equip",
            "Customize",
            "ResetCustomization",
            "ReplaceUnlockedVehicleIds",
            "RequireUnlocked",
            "Garage vehicle '{vehicleId}' is locked.",
            "StateChanged",
        ):
            self.assertIn(required, source)

    def test_persistence_is_versioned_and_has_migration_and_recovery(self):
        source = (GARAGE / "GaragePersistence.cs").read_text(encoding="utf-8")
        for required in (
            'CurrentHeader = "AFAREET_GARAGE_V2"',
            'LegacyHeaderV1 = "AFAREET_GARAGE_V1"',
            "MigratedLegacyV1",
            "RecoveredFromInvalidPayload",
            "GarageStateStore",
            "IGarageStateStorage",
            "EncodeLegacyV1ForMigrationFixture",
        ):
            self.assertIn(required, source)

        player_prefs = (GARAGE / "PlayerPrefsGarageStateStorage.cs").read_text(encoding="utf-8")
        self.assertIn('DefaultKey = "afareet.garage.state.v2"', player_prefs)
        self.assertIn("PlayerPrefs.Save()", player_prefs)

    def test_career_unlocks_are_validated_against_garage_catalog(self):
        bridge = CAREER_BRIDGE.read_text(encoding="utf-8")
        for required in (
            "ResolveUnlockedVehicleIds",
            "CreateGarageService",
            "ValidateCareerVehicleRewardsOrThrow",
            "has no Garage catalog definition",
            "unlocks unknown Garage vehicle",
        ):
            self.assertIn(required, bridge)

        asmdef = CAREER_ASMDEF.read_text(encoding="utf-8")
        self.assertIn('"Afareet.GarageRuntime"', asmdef)

    def test_career_garage_session_is_single_runtime_persistence_boundary(self):
        self.assertTrue(CAREER_SESSION.is_file())
        source = CAREER_SESSION.read_text(encoding="utf-8")
        for required in (
            "CareerGameSession career",
            "GarageStateStore stateStore",
            "GarageService garage",
            "ConfigureWithPlayerPrefs",
            "career.ProgressChanged += OnCareerProgressChanged",
            "RefreshUnlocksFromCareer",
            "stateStore.Save",
            "GarageStateChanged",
            "GarageUnlocksChanged",
        ):
            self.assertIn(required, source)

    def test_runtime_bootstrap_composes_career_garage_session_after_career(self):
        source = BOOTSTRAP.read_text(encoding="utf-8")
        career_configure = source.index("career.Configure(")
        garage_add = source.index("gameObject.AddComponent<CareerGarageSession>()")
        garage_configure = source.index("garage.ConfigureWithPlayerPrefs(career)")
        self.assertLess(career_configure, garage_add)
        self.assertLess(garage_add, garage_configure)
        self.assertIn("AFAREET_GARAGE_SESSION_ACTIVE", source)

    def test_pure_csharp_compile_and_behavior_contract_is_wired(self):
        for path in (COMPILE_PROJECT, RUNNER_PROJECT, RUNNER, WORKFLOW):
            self.assertTrue(path.is_file(), path.name)

        compile_project = COMPILE_PROJECT.read_text(encoding="utf-8")
        for source_name in (
            "GarageCatalog.cs",
            "GarageState.cs",
            "GarageService.cs",
            "GaragePersistence.cs",
        ):
            self.assertIn(source_name, compile_project)
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", compile_project)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", compile_project)

        runner_project = RUNNER_PROJECT.read_text(encoding="utf-8")
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("GarageRuntimeContractRunner.cs", runner_project)
        runner = RUNNER.read_text(encoding="utf-8")
        for required in (
            "CatalogContract();",
            "UnlockEquipContract();",
            "CustomizationContract();",
            "PersistenceContract();",
            "MigrationRecoveryContract();",
            "Garage runtime behavior contract: PASS",
        ):
            self.assertIn(required, runner)

        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("dotnet build tools/android/contracts/GarageRuntimeCompile.csproj", workflow)
        self.assertIn("GarageRuntimeContractRunner.csproj", workflow)
        self.assertIn("test_post_p1_garage_runtime_contract.py", workflow)

    def test_garage_programming_does_not_fake_missing_visual_assets(self):
        catalog = (GARAGE / "GarageCatalog.cs").read_text(encoding="utf-8")
        service = (GARAGE / "GarageService.cs").read_text(encoding="utf-8")
        preview = (GARAGE / "GaragePreviewResolver.cs").read_text(encoding="utf-8")
        session = CAREER_SESSION.read_text(encoding="utf-8")
        combined = catalog + service + preview + session
        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh(",
            "PrimitiveType.Cube",
            "PrimitiveType.Sphere",
            "PrimitiveType.Cylinder",
        ):
            self.assertNotIn(forbidden, combined)


if __name__ == "__main__":
    unittest.main()
