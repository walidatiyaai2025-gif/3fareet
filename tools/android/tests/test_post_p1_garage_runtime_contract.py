import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
GARAGE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/GarageRuntime"
CAREER_BRIDGE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerGarageBridge.cs"
CAREER_ASMDEF = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/CareerRuntime/Afareet.CareerRuntime.asmdef"


class PostP1GarageRuntimeContractTests(unittest.TestCase):
    def test_garage_runtime_module_covers_catalog_state_service_and_persistence(self):
        required_files = (
            "Afareet.GarageRuntime.asmdef",
            "GarageCatalog.cs",
            "GarageState.cs",
            "GarageService.cs",
            "GaragePersistence.cs",
            "PlayerPrefsGarageStateStorage.cs",
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

    def test_garage_programming_does_not_fake_missing_visual_assets(self):
        catalog = (GARAGE / "GarageCatalog.cs").read_text(encoding="utf-8")
        service = (GARAGE / "GarageService.cs").read_text(encoding="utf-8")
        combined = catalog + service
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
