import importlib.util
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO / "tools" / "android" / "p1_licensed_staging_readiness.py"
SPEC = importlib.util.spec_from_file_location("p1_licensed_staging_readiness", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def run_git(root: Path, *args: str) -> None:
    subprocess.run(["git", "-C", str(root), *args], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)


def make_fixture() -> Path:
    temp = Path(tempfile.mkdtemp(prefix="afareet-staging-readiness-"))
    run_git(temp, "init")
    run_git(temp, "config", "user.email", "qa@example.invalid")
    run_git(temp, "config", "user.name", "P1 QA")

    required = set(MODULE.RIVAL_REQUIRED_FILES) | set(MODULE.HANDOFF_REQUIRED_FILES) | set(MODULE.WORLD_REQUIRED_FILES)
    required.add("unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
    for relative in required:
        path = temp / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("fixture\n", encoding="utf-8")

    run_git(temp, "add", ".")
    run_git(temp, "commit", "-m", "fixture")
    return temp


class P1LicensedStagingReadinessTests(unittest.TestCase):
    def test_current_repo_is_fail_closed_without_explicit_hero_source(self):
        report = MODULE.audit(REPO, hero_source=None, require_clean=False)

        self.assertEqual("BLOCKED", report["state"])
        self.assertFalse(report["readyForLicensedStaging"])
        self.assertFalse(report["candidateBuildStarted"])
        self.assertFalse(report["publicationEligible"])
        self.assertFalse(report["verified"])
        self.assertIn("UART-003_HERO_SOURCE_SUPPLIED", report["blockedCheckIds"])

        unexpected = [
            item
            for item in report["blockedCheckIds"]
            if item.startswith("RIVAL:") or item.startswith("HANDOFF:") or item.startswith("WORLD:")
        ]
        self.assertEqual([], unexpected, f"Existing staging support drifted unexpectedly: {unexpected}")

    def test_complete_clean_tracked_fixture_is_ready_for_licensed_staging_only(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("READY_FOR_LICENSED_STAGING", report["state"])
            self.assertTrue(report["readyForLicensedStaging"])
            self.assertEqual([], report["blockedCheckIds"])
            self.assertFalse(report["candidateBuildStarted"])
            self.assertFalse(report["publicationEligible"])
            self.assertFalse(report["verified"])
        finally:
            import shutil
            shutil.rmtree(root, ignore_errors=True)

    def test_dirty_tree_blocks_staging_readiness(self):
        root = make_fixture()
        try:
            (root / "dirty.txt").write_text("not committed\n", encoding="utf-8")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("CLEAN_TREE", report["blockedCheckIds"])
        finally:
            import shutil
            shutil.rmtree(root, ignore_errors=True)

    def test_preview_or_generated_hero_path_is_rejected_even_if_tracked(self):
        root = make_fixture()
        try:
            preview = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/Preview/AfareetKing_Generated.fbx"
            preview.parent.mkdir(parents=True, exist_ok=True)
            preview.write_text("preview\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "add preview")

            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/Preview/AfareetKing_Generated.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_PREVIEW_OR_BLOCKOUT", report["blockedCheckIds"])
        finally:
            import shutil
            shutil.rmtree(root, ignore_errors=True)

    def test_rival_source_cannot_be_reused_as_hero(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_RIVAL_SOURCE", report["blockedCheckIds"])
        finally:
            import shutil
            shutil.rmtree(root, ignore_errors=True)

    def test_report_output_is_confined_to_ignored_artifacts_root(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source=None, require_clean=False)
            good = root / "artifacts" / "p1" / "licensed-staging-readiness.json"
            MODULE._write_report(root, good, report)
            self.assertTrue(good.is_file())
            payload = json.loads(good.read_text(encoding="utf-8"))
            self.assertEqual("BLOCKED", payload["state"])

            with self.assertRaises(ValueError):
                MODULE._write_report(root, root / "docs" / "readiness.json", report)
        finally:
            import shutil
            shutil.rmtree(root, ignore_errors=True)

    def test_assets_prefix_normalizes_to_unity_project_path(self):
        self.assertEqual(
            "unity_game/Assets/Afareet/Hero.fbx",
            MODULE._normalize_hero_path("Assets/Afareet/Hero.fbx"),
        )


if __name__ == "__main__":
    unittest.main()
