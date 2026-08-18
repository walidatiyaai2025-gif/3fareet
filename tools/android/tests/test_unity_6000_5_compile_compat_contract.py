import json
import re
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]

PARENT_EDITMODE_ASMDEF = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Afareet.EditModeTests.asmdef"
PROGRESSION_EDITMODE_ASMDEF = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/Afareet.ProgressionEditModeTests.asmdef"
PROGRESSION_TESTS = [
    REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerContentTests.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerProgressionTests.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerSaveCodecTests.cs",
]
RUNTIME_ID_FILES = [
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadsideClutterRuntimePass.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadCurbMobileLodRuntimePass.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs",
]
TRACK_BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"
RIVAL_VARIANT_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs"


class Unity60005CompileCompatContractTests(unittest.TestCase):
    def test_parent_editmode_keeps_vehicle_only_dependency(self):
        data = json.loads(PARENT_EDITMODE_ASMDEF.read_text(encoding="utf-8-sig"))
        self.assertEqual(data.get("references", []), ["Afareet.Vehicle"])

    def test_progression_test_assembly_references_progression(self):
        data = json.loads(PROGRESSION_EDITMODE_ASMDEF.read_text(encoding="utf-8-sig"))
        self.assertIn("Afareet.Progression", data.get("references", []))

    def test_progression_tests_import_runtime_namespace(self):
        for path in PROGRESSION_TESTS:
            source = path.read_text(encoding="utf-8-sig")
            self.assertIn(
                "using Afareet.Progression;",
                source,
                msg=f"{path.relative_to(REPO_ROOT)} must import Afareet.Progression",
            )

    def test_runtime_passes_do_not_call_obsolete_get_instance_id(self):
        for path in RUNTIME_ID_FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertNotIn(
                ".GetInstanceID()",
                source,
                msg=f"{path.relative_to(REPO_ROOT)} still uses Unity 6000.5-obsolete GetInstanceID",
            )

    def test_track_builder_has_no_ambiguous_object_destroy_calls(self):
        source = TRACK_BUILDER.read_text(encoding="utf-8-sig")
        ambiguous = re.findall(r"(?<![\w.])Object\.Destroy\(", source)
        self.assertEqual(ambiguous, [])

    def test_rival_variant_reason_is_initialized_before_short_circuit_validation(self):
        source = RIVAL_VARIANT_PASS.read_text(encoding="utf-8-sig")
        self.assertRegex(
            source,
            r'var\s+reason\s*=\s*"missing-production-prefab";\s*'
            r'if\s*\(prefab\s*!=\s*null\s*&&\s*'
            r'RivalProductionPolicy\.ValidateProductionPrefab\(prefab,\s*index,\s*out\s+reason\)\)',
        )
        self.assertNotIn("out var reason", source)


if __name__ == "__main__":
    unittest.main()
