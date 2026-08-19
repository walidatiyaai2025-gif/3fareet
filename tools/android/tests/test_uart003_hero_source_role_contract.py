import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
METADATA = REPO / "unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionAssetMetadata.cs"


class Uart003HeroSourceRoleContractTests(unittest.TestCase):
    def test_hero_source_must_be_vehicle_role_and_never_rival_role(self):
        text = METADATA.read_text(encoding="utf-8")
        self.assertIn('VehiclePathMarker = "/Vehicles/"', text)
        self.assertIn("IndexOf(VehiclePathMarker", text)
        self.assertIn('"/Rivals/"', text)
        self.assertIn("NonProductionSourceMarkers", text)

    def test_role_policy_remains_central_for_all_production_entry_points(self):
        for relative in (
            "unity_game/Assets/Afareet/Editor/HeroCarProductionPrefabStager.cs",
            "unity_game/Assets/Afareet/Editor/HeroCarProductionSourceBinder.cs",
            "unity_game/Assets/Afareet/Editor/HeroCarProductionBuildPreprocessor.cs",
        ):
            text = (REPO / relative).read_text(encoding="utf-8")
            self.assertIn("HeroCarProductionAssetMetadata.IsSupportedExternalModelSource", text, relative)


if __name__ == "__main__":
    unittest.main()
