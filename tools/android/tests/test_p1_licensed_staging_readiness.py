import importlib.util
import json
import shutil
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


def write_rival_policy(root: Path) -> None:
    policy = root / MODULE.RIVAL_POLICY_FILE
    policy.parent.mkdir(parents=True, exist_ok=True)
    policy.write_text(
        '''namespace Afareet.Vehicle
{
    internal static class RivalProductionPolicy
    {
        internal const string ProductionSourceRoot = "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/";
        internal static readonly string[] ProductionSources =
        {
            ProductionSourceRoot + "Rival_01_WedgeCoupe_Production.obj",
            ProductionSourceRoot + "Rival_02_FastbackMuscle_Production.obj",
            ProductionSourceRoot + "Rival_03_CompactPrototype_Production.obj",
        };
        internal static readonly int[] MinimumTriangles = { 1800, 800, 350 };
        internal static readonly int[] MaximumTriangles = { 16000, 8000, 4000 };
    }
}
''',
        encoding="utf-8",
    )


def write_valid_rival_handoff(root: Path) -> None:
    package = root / MODULE.RIVAL_PRODUCTION_ROOT
    materials = package / "materials"
    textures = package / "textures"
    materials.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    triangle_counts = (1800, 800, 350)

    for variant, relative_obj in enumerate(MODULE.RIVAL_OBJ_FILES, start=1):
        obj = root / relative_obj
        obj.parent.mkdir(parents=True, exist_ok=True)
        lines = [
            f"# readiness fixture variant {variant}",
            f"mtllib materials/rival_{variant}.mtl",
            "v 0 0 0",
            "v 1 0 0",
            "v 0 1 0",
            "vt 0 0",
            "vt 1 0",
            "vt 0 1",
            "vn 0 0 1",
        ]
        for lod, triangle_count in enumerate(triangle_counts):
            lines.append(f"o Rival_{variant:02}_LOD{lod}")
            lines.append(f"usemtl Mat_LOD{lod}")
            lines.extend(["f 1/1/1 2/2/1 3/3/1"] * triangle_count)
        obj.write_text("\n".join(lines) + "\n", encoding="utf-8")

        mtl = materials / f"rival_{variant}.mtl"
        mtl.write_text(
            "\n".join(
                line for lod in range(3)
                for line in (f"newmtl Mat_LOD{lod}", "Kd 1 1 1", f"map_Kd ../textures/rival_{variant}.png")
            ) + "\n",
            encoding="utf-8",
        )
        Path(str(mtl) + ".meta").write_text(f"guid: mtl{variant}\n", encoding="utf-8")
        texture = textures / f"rival_{variant}.png"
        texture.write_bytes(f"texture-{variant}".encode("ascii"))
        Path(str(texture) + ".meta").write_text(f"guid: texture{variant}\n", encoding="utf-8")


def make_fixture() -> Path:
    temp = Path(tempfile.mkdtemp(prefix="afareet-staging-readiness-"))
    run_git(temp, "init")
    run_git(temp, "config", "user.email", "qa@example.invalid")
    run_git(temp, "config", "user.name", "P1 QA")

    hero = "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
    required = set(MODULE.RIVAL_REQUIRED_FILES) | set(MODULE.HANDOFF_REQUIRED_FILES) | set(MODULE.WORLD_REQUIRED_FILES)
    required.add(hero)
    required.add(hero + ".meta")
    for relative in required:
        path = temp / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("fixture\n", encoding="utf-8")

    write_rival_policy(temp)
    write_valid_rival_handoff(temp)
    run_git(temp, "add", ".")
    run_git(temp, "commit", "-m", "fixture")
    return temp


class P1LicensedStagingReadinessTests(unittest.TestCase):
    def test_current_repo_is_fail_closed_without_hero_and_isolated_rival_production_sources(self):
        report = MODULE.audit(REPO, hero_source=None, require_clean=False)
        self.assertEqual(2, report["schemaVersion"])
        self.assertEqual("BLOCKED", report["state"])
        self.assertFalse(report["readyForLicensedStaging"])
        self.assertFalse(report["candidateBuildStarted"])
        self.assertFalse(report["publicationEligible"])
        self.assertFalse(report["runtimeVerified"])
        self.assertFalse(report["ownerAccepted"])
        self.assertFalse(report["verified"])
        self.assertIn("UART-003_HERO_SOURCE_SUPPLIED", report["blockedCheckIds"])
        self.assertIn("UART-004_RIVAL_HANDOFF_STRUCTURE", report["blockedCheckIds"])
        rival_blockers = [item for item in report["blockedCheckIds"] if item.startswith("RIVAL:")]
        self.assertEqual(list(MODULE.RIVAL_REQUIRED_FILES), [item.removeprefix("RIVAL:") for item in rival_blockers])
        for blocker in rival_blockers:
            self.assertIn("/Rivals/Production/", blocker)
        unexpected = [item for item in report["blockedCheckIds"] if item.startswith("HANDOFF:") or item.startswith("WORLD:")]
        self.assertEqual([], unexpected, f"Existing convergence support drifted unexpectedly: {unexpected}")

    def test_complete_clean_tracked_fixture_is_ready_for_licensed_staging_only(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("READY_FOR_LICENSED_STAGING", report["state"])
            self.assertTrue(report["readyForLicensedStaging"])
            self.assertEqual([], report["blockedCheckIds"])
            check_ids = [item["id"] for item in report["checks"]]
            self.assertIn("UART-003_HERO_HANDOFF_PREFLIGHT", check_ids)
            self.assertIn("UART-004_RIVAL_HANDOFF_STRUCTURE", check_ids)
            self.assertIn("UART-004_RIVAL_DEPENDENCY_SET", check_ids)
            self.assertTrue(any(item.startswith("RIVAL_DEP:") for item in check_ids))
            hero_check = next(item for item in report["checks"] if item["id"] == "UART-003_HERO_HANDOFF_PREFLIGHT")
            self.assertIn("verdict=UNITY_INSPECTION_REQUIRED", hero_check["detail"])
            self.assertIn("unityInspectionRequired=true", hero_check["detail"])
            for key in ("candidateBuildStarted", "publicationEligible", "runtimeVerified", "ownerAccepted", "verified"):
                self.assertFalse(report[key], key)
            self.assertIn("preflighted tracked Hero source", report["nextAction"])
            self.assertIn("opaque Hero formats still require Unity importer inspection", report["nextAction"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_invalid_tracked_hero_obj_blocks_handoff_preflight(self):
        root = make_fixture()
        try:
            hero = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/Bad_Production.obj"
            hero.write_text("v 0 0 0\n", encoding="utf-8")
            Path(str(hero) + ".meta").write_text("guid: badobj\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "bad hero obj")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/Bad_Production.obj")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_HANDOFF_PREFLIGHT", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_invalid_rival_obj_structure_blocks_readiness_even_when_base_files_are_tracked(self):
        root = make_fixture()
        try:
            rival = root / MODULE.RIVAL_OBJ_FILES[0]
            rival.write_text("fixture but not a valid OBJ handoff\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "break rival structure")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-004_RIVAL_HANDOFF_STRUCTURE", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_missing_tracked_texture_meta_blocks_readiness_after_handoff_structure_passes(self):
        root = make_fixture()
        try:
            texture_meta = root / MODULE.RIVAL_PRODUCTION_ROOT / "textures/rival_1.png.meta"
            texture_meta.unlink()
            run_git(root, "add", "-u")
            run_git(root, "commit", "-m", "remove texture meta")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            expected = f"RIVAL_DEP:{MODULE.RIVAL_PRODUCTION_ROOT}/textures/rival_1.png.meta"
            self.assertEqual("BLOCKED", report["state"])
            self.assertNotIn("UART-004_RIVAL_HANDOFF_STRUCTURE", report["blockedCheckIds"])
            self.assertIn(expected, report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_dirty_tree_blocks_staging_readiness(self):
        root = make_fixture()
        try:
            (root / "dirty.txt").write_text("not committed\n", encoding="utf-8")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("CLEAN_TREE", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_preview_generated_refinement_or_review_hero_path_is_rejected_even_if_tracked(self):
        root = make_fixture()
        try:
            preview = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/Refinement/AfareetKing_Generated.fbx"
            preview.parent.mkdir(parents=True, exist_ok=True)
            preview.write_text("preview\n", encoding="utf-8")
            Path(str(preview) + ".meta").write_text("meta\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "add nonproduction hero")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/Refinement/AfareetKing_Generated.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_NONPRODUCTION_PATH", report["blockedCheckIds"])
            self.assertIn("UART-003_HERO_HANDOFF_PREFLIGHT", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_nonvehicle_hero_role_is_rejected_even_if_tracked(self):
        root = make_fixture()
        try:
            source = root / "unity_game/Assets/Afareet/ArtSource/Characters/Hero/AfareetKing_Production.fbx"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_text("hero\n", encoding="utf-8")
            Path(str(source) + ".meta").write_text("meta\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "add wrong-role hero")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Characters/Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_VEHICLE_ROLE", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_traversal_hero_path_is_rejected_before_staging(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/../Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NO_TRAVERSAL", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_rival_source_cannot_be_reused_as_hero(self):
        root = make_fixture()
        try:
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_RIVAL_SOURCE", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_missing_hero_meta_blocks_staging(self):
        root = make_fixture()
        try:
            meta = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx.meta"
            meta.unlink()
            run_git(root, "add", "-u")
            run_git(root, "commit", "-m", "remove hero meta")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_META_TRACKED_BY_HEAD", report["blockedCheckIds"])
            self.assertIn("UART-003_HERO_HANDOFF_PREFLIGHT", report["blockedCheckIds"])
        finally:
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
            shutil.rmtree(root, ignore_errors=True)

    def test_assets_prefix_normalizes_to_unity_project_path(self):
        self.assertEqual("unity_game/Assets/Afareet/Hero.fbx", MODULE._normalize_hero_path("Assets/Afareet/Hero.fbx"))


if __name__ == "__main__":
    unittest.main()
