import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
SCRIPT = REPO / "tools" / "android" / "validate_hero_asset_intake_windows.ps1"
POLICY = """using System;\nnamespace Afareet.Vehicle { public static class HeroCarLodPolicy {\npublic static readonly int[] MinimumVertices = { 1, 1, 1 };\npublic static readonly int[] VertexBudgets = { 100, 100, 100 };\npublic static readonly int[] MinimumTriangles = { 1, 1, 1 };\npublic static readonly int[] TriangleBudgets = { 100, 100, 100 };\n} }\n"""


def run_git(root: Path, *args: str) -> None:
    subprocess.run(
        ["git", "-C", str(root), *args],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )


def make_repo() -> Path:
    root = Path(tempfile.mkdtemp(prefix="afareet-native-hero-intake-"))
    run_git(root, "init")
    run_git(root, "config", "user.email", "qa@example.invalid")
    run_git(root, "config", "user.name", "P1 QA")
    policy = root / "unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs"
    policy.parent.mkdir(parents=True, exist_ok=True)
    policy.write_text(POLICY, encoding="utf-8")
    return root


def commit_all(root: Path, message: str = "fixture") -> None:
    run_git(root, "add", ".")
    run_git(root, "commit", "-m", message)


def invoke(root: Path, source: str, output: Path | None = None) -> subprocess.CompletedProcess[str]:
    pwsh = shutil.which("pwsh")
    if not pwsh:
        raise unittest.SkipTest("pwsh is not installed in this environment")
    args = [pwsh, "-NoProfile", "-File", str(SCRIPT), "-Source", source, "-RepoRoot", str(root)]
    if output is not None:
        args.extend(["-Output", str(output)])
    return subprocess.run(args, capture_output=True, text=True)


class NativeHeroAssetIntakeTests(unittest.TestCase):
    def test_script_parses_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is not installed in this environment")
        command = (
            "$tokens=$null; $errors=$null; "
            f"[System.Management.Automation.Language.Parser]::ParseFile('{SCRIPT.as_posix()}', "
            "[ref]$tokens, [ref]$errors) | Out-Null; "
            "if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_.Message }; exit 1 }"
        )
        result = subprocess.run([pwsh, "-NoProfile", "-Command", command], capture_output=True, text=True)
        self.assertEqual(0, result.returncode, msg=result.stdout + result.stderr)

    def test_tracked_binary_source_requires_unity_inspection_and_never_approves(self):
        root = make_repo()
        try:
            source = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.fbx"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(b"fixture-fbx")
            commit_all(root)
            report_path = root / "artifacts" / "native-intake.json"
            result = invoke(
                root,
                "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.fbx",
                report_path,
            )
            self.assertEqual(0, result.returncode, msg=result.stdout + result.stderr)
            report = json.loads(report_path.read_text(encoding="utf-8-sig"))
            self.assertEqual("UNITY_INSPECTION_REQUIRED", report["verdict"])
            self.assertEqual("BINARY_OR_DCC_SOURCE_NOT_INSPECTED", report["sourceInspection"])
            self.assertFalse(report["verified"])
            self.assertFalse(report["productionArtApproved"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_malformed_obj_is_rejected_before_unity(self):
        root = make_repo()
        try:
            source = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.obj"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_text(
                "mtllib AfareetKing.mtl\n"
                "o AfareetKing_LOD0\n"
                "v 0 0 0\nv 1 0 0\nv 0 1 0\n"
                "vt 0 0\nvt 1 0\nvt 0 1\n"
                "vn 0 0 1\n"
                "usemtl HeroBody\n"
                "f 1/1/1 2/2/1 3/3/1\n",
                encoding="utf-8",
            )
            (source.parent / "AfareetKing.mtl").write_text(
                "newmtl HeroBody\nmap_Kd HeroBody.png\n", encoding="utf-8"
            )
            (source.parent / "HeroBody.png").write_bytes(b"png-fixture")
            commit_all(root)
            result = invoke(root, "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing_Production.obj")
            self.assertNotEqual(0, result.returncode, msg=result.stdout + result.stderr)
            self.assertIn("missing object/group suffix _LOD1", result.stdout + result.stderr)
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_forbidden_generated_segment_is_rejected(self):
        root = make_repo()
        try:
            source = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/Generated/AfareetKing.fbx"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(b"fixture")
            commit_all(root)
            result = invoke(root, "Assets/Afareet/ArtSource/Vehicles/HeroCar/Generated/AfareetKing.fbx")
            self.assertNotEqual(0, result.returncode, msg=result.stdout + result.stderr)
            self.assertIn("forbidden path segment: generated", (result.stdout + result.stderr).lower())
        finally:
            shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
