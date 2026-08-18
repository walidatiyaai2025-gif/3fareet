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


def copy_repo_file(root: Path, relative: str) -> None:
    source = REPO / relative
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if source.is_file():
        shutil.copy2(source, target)
    else:
        target.write_text("fixture\n", encoding="utf-8")


def write_meta(path: Path, guid_seed: str) -> None:
    path.write_text(f"fileFormatVersion: 2\nguid: {guid_seed[:32].ljust(32, '0')}\n", encoding="utf-8")


def write_rival_production_fixture(root: Path) -> None:
    policy_relative = MODULE.p1_visual_source_readiness.RIVAL_POLICY
    policy_path = root / policy_relative
    validator = MODULE.p1_visual_source_readiness.validate_uart004_rival_production_handoff
    policy = validator.parse_policy(policy_path)

    production_dir = root / ("unity_game/" + str(policy["productionSourceRoot"]).lstrip("/"))
    production_dir.mkdir(parents=True, exist_ok=True)

    texture = production_dir / "rival_albedo.png"
    texture.write_bytes(b"rival-production-texture-fixture")
    write_meta(texture.with_name(texture.name + ".meta"), "aa01")

    for variant, source_name in enumerate(policy["sourceFileNames"]):
        mtl_name = f"rival_{variant + 1}.mtl"
        mtl_path = production_dir / mtl_name
        mtl_lines = []
        for lod in range(3):
            mtl_lines.extend(
                (
                    f"newmtl Mat_LOD{lod}",
                    "Kd 1 1 1",
                    "map_Kd rival_albedo.png",
                )
            )
        mtl_path.write_text("\n".join(mtl_lines) + "\n", encoding="utf-8")
        write_meta(mtl_path.with_name(mtl_path.name + ".meta"), f"bb{variant + 1:02}")

        lines = [
            f"# production fixture variant {variant + 1}",
            f"mtllib {mtl_name}",
            "v 0 0 0",
            "v 1 0 0",
            "v 0 1 0",
            "vt 0 0",
            "vt 1 0",
            "vt 0 1",
            "vn 0 0 1",
        ]
        for lod, count in enumerate(policy["MinimumTriangles"]):
            lines.append(f"o Rival_{variant + 1:02}_LOD{lod}")
            lines.append(f"usemtl Mat_LOD{lod}")
            lines.extend(["f 1/1/1 2/2/1 3/3/1"] * count)

        obj_path = production_dir / source_name
        obj_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        write_meta(obj_path.with_name(obj_path.name + ".meta"), f"cc{variant + 1:02}")


def make_fixture() -> Path:
    temp = Path(tempfile.mkdtemp(prefix="afareet-staging-readiness-"))
    run_git(temp, "init")
    run_git(temp, "config", "user.email", "qa@example.invalid")
    run_git(temp, "config", "user.name", "P1 QA")

    for relative in set(MODULE.RIVAL_REQUIRED_FILES) | set(MODULE.HANDOFF_REQUIRED_FILES) | set(MODULE.WORLD_REQUIRED_FILES):
        copy_repo_file(temp, relative)

    for relative in MODULE.p1_visual_source_readiness.URAC011_FILES:
        copy_repo_file(temp, relative)

    for relative_manifest in MODULE.p1_visual_source_readiness.MANIFESTS.values():
        manifest = json.loads((REPO / relative_manifest).read_text(encoding="utf-8"))
        source_root = manifest["sourceRoot"]
        source_dir = REPO / source_root
        target_dir = temp / source_root
        target_dir.parent.mkdir(parents=True, exist_ok=True)
        shutil.copytree(source_dir, target_dir, dirs_exist_ok=True)

    hero = temp / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.fbx"
    hero.parent.mkdir(parents=True, exist_ok=True)
    hero.write_text("fixture-fbx\n", encoding="utf-8")
    write_meta(hero.with_name(hero.name + ".meta"), "dd01")

    policy = temp / MODULE.validate_hero_asset_intake.POLICY_PATH
    policy.parent.mkdir(parents=True, exist_ok=True)
    policy.write_text(
        "\n".join(
            [
                "public static class HeroCarLodPolicy",
                "{",
                "    public static readonly int[] MinimumVertices = { 1500, 800, 500 };",
                "    public static readonly int[] VertexBudgets = { 5000, 2800, 1800 };",
                "    public static readonly int[] MinimumTriangles = { 3500, 1600, 900 };",
                "    public static readonly int[] TriangleBudgets = { 7500, 4000, 2500 };",
                "}",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    write_rival_production_fixture(temp)

    run_git(temp, "add", ".")
    run_git(temp, "commit", "-m", "fixture")
    return temp


class P1LicensedStagingReadinessTests(unittest.TestCase):
    def test_current_repo_is_fail_closed_without_external_hero_or_rival_production_sources(self):
        report = MODULE.audit(REPO, hero_source=None, require_clean=False)

        self.assertEqual("BLOCKED", report["state"])
        self.assertFalse(report["readyForLicensedStaging"])
        self.assertFalse(report["candidateBuildStarted"])
        self.assertFalse(report["publicationEligible"])
        self.assertFalse(report["verified"])
        self.assertIn("UART-003_HERO_SOURCE_SUPPLIED", report["blockedCheckIds"])
        self.assertEqual(["UART-003", "UART-004"], report["visualSourceBlockedTaskIds"])
        self.assertEqual(4, report["visualSourceReadyCount"])

        rival_blockers = [item for item in report["blockedCheckIds"] if item.startswith("RIVAL:")]
        self.assertEqual(list(MODULE.RIVAL_REQUIRED_FILES), [item.removeprefix("RIVAL:") for item in rival_blockers])
        self.assertFalse(any(item.startswith("HANDOFF:") for item in report["blockedCheckIds"]))
        self.assertFalse(any(item.startswith("WORLD:") for item in report["blockedCheckIds"]))

    def test_complete_clean_tracked_fixture_is_ready_for_licensed_staging_only(self):
        root = make_fixture()
        try:
            hero = "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.fbx"
            report = MODULE.audit(root, hero_source=hero)
            self.assertEqual("READY_FOR_LICENSED_STAGING", report["state"])
            self.assertTrue(report["readyForLicensedStaging"])
            self.assertEqual([], report["blockedCheckIds"])
            self.assertEqual("READY_FOR_LICENSED_VISUAL_STAGING", report["visualSourceState"])
            self.assertEqual(6, report["visualSourceReadyCount"])
            self.assertEqual([], report["visualSourceBlockedTaskIds"])
            self.assertFalse(report["candidateBuildStarted"])
            self.assertFalse(report["publicationEligible"])
            self.assertFalse(report["verified"])
            intake = next(item for item in report["checks"] if item["id"] == "UART-003_HERO_INTAKE")
            self.assertEqual("PASS", intake["status"])
            self.assertIn("licensed Unity inspection", intake["detail"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_tracked_malformed_obj_blocks_before_licensed_staging(self):
        root = make_fixture()
        try:
            source = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.obj"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_text(
                "\n".join(
                    [
                        "mtllib hero.mtl",
                        "v 0 0 0",
                        "v 1 0 0",
                        "v 0 1 0",
                        "o AfareetKing_LOD0",
                        "usemtl Hero",
                        "f 1 2 3",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            write_meta(source.with_name(source.name + ".meta"), "ee01")
            (source.parent / "hero.mtl").write_text("newmtl Hero\nKd 1 1 1\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "add malformed obj hero")

            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.obj")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_INTAKE", report["blockedCheckIds"])
            self.assertIn("VISUAL_SOURCE:UART-003", report["blockedCheckIds"])
            self.assertNotIn("VISUAL_SOURCE:UART-004", report["blockedCheckIds"])
            intake = next(item for item in report["checks"] if item["id"] == "UART-003_HERO_INTAKE")
            self.assertIn("missing object/group suffix _LOD1", intake["detail"])
            self.assertFalse(report["candidateBuildStarted"])
            self.assertFalse(report["publicationEligible"])
            self.assertFalse(report["verified"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_dirty_tree_blocks_staging_readiness(self):
        root = make_fixture()
        try:
            (root / "dirty.txt").write_text("not committed\n", encoding="utf-8")
            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("CLEAN_TREE", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_preview_or_generated_hero_path_is_rejected_even_if_tracked(self):
        root = make_fixture()
        try:
            preview = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/Preview/AfareetKing_Generated.fbx"
            preview.parent.mkdir(parents=True, exist_ok=True)
            preview.write_text("preview\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-m", "add preview")

            report = MODULE.audit(root, hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/Preview/AfareetKing_Generated.fbx")
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_PREVIEW_OR_BLOCKOUT", report["blockedCheckIds"])
            self.assertIn("VISUAL_SOURCE:UART-003", report["blockedCheckIds"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_rival_source_cannot_be_reused_as_hero(self):
        root = make_fixture()
        try:
            report = MODULE.audit(
                root,
                hero_source="Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj",
            )
            self.assertEqual("BLOCKED", report["state"])
            self.assertIn("UART-003_HERO_NOT_RIVAL_SOURCE", report["blockedCheckIds"])
            self.assertIn("VISUAL_SOURCE:UART-003", report["blockedCheckIds"])
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
        self.assertEqual(
            "unity_game/Assets/Afareet/Hero.fbx",
            MODULE._normalize_hero_path("Assets/Afareet/Hero.fbx"),
        )


if __name__ == "__main__":
    unittest.main()
