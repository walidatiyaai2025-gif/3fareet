import json
import re
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]

EDITMODE_ASMDEF = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Afareet.EditModeTests.asmdef"
RUNTIME_ID_FILES = [
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadsideClutterRuntimePass.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadCurbMobileLodRuntimePass.cs",
    REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs",
]
TRACK_BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"


class Unity60005CompileCompatContractTests(unittest.TestCase):
    def test_editmode_tests_reference_progression_assembly(self):
        data = json.loads(EDITMODE_ASMDEF.read_text(encoding="utf-8-sig"))
        self.assertIn("Afareet.Vehicle", data.get("references", []))
        self.assertIn("Afareet.Progression", data.get("references", []))

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
        self.assertEqual(
            ambiguous,
            [],
            msg="CairoTrackBuilder must qualify UnityEngine.Object.Destroy when System and UnityEngine are both imported",
        )


if __name__ == "__main__":
    unittest.main()
