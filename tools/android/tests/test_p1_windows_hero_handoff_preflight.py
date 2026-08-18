import importlib.util
import shutil
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
NATIVE = REPO_ROOT / "tools/android/hero_production_handoff_preflight_windows.ps1"
STAGING = REPO_ROOT / "tools/android/stage_production_candidate_windows.ps1"
HELPER_PATH = Path(__file__).with_name("test_uart003_hero_production_handoff.py")
SPEC = importlib.util.spec_from_file_location("hero_handoff_fixture", HELPER_PATH)
HELPER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
previous_module = sys.modules.get(SPEC.name)
sys.modules[SPEC.name] = HELPER
try:
    SPEC.loader.exec_module(HELPER)
except BaseException:
    if previous_module is None:
        sys.modules.pop(SPEC.name, None)
    else:
        sys.modules[SPEC.name] = previous_module
    raise


class WindowsHeroHandoffPreflightTests(unittest.TestCase):
    @staticmethod
    def run_command(command):
        return subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)

    def test_staging_runs_native_hero_gate_before_rival_and_unity(self):
        source = STAGING.read_text(encoding="utf-8")
        for required in (
            "hero_production_handoff_preflight_windows.ps1",
            "AFAREET_STAGING_HERO_HANDOFF_PREFLIGHT_START",
            "AFAREET_STAGING_HERO_HANDOFF_PREFLIGHT_OK",
            "mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)
        hero = source.index("AFAREET_STAGING_HERO_HANDOFF_PREFLIGHT_START")
        rival = source.index("AFAREET_STAGING_RIVAL_DEPENDENCY_PREFLIGHT_START")
        unity = source.index("Start-Process -FilePath $UnityPath")
        self.assertLess(hero, rival)
        self.assertLess(hero, unity)
        self.assertNotIn("python", source.lower())

    def test_native_contract_uses_current_source_policy_and_token_safe_lods(self):
        source = NATIVE.read_text(encoding="utf-8")
        for required in (
            "placeholder", "legacyprocedural", "refinementcandidates", "reviewpackaging",
            "/vehicles/", "/rivals/", "Resolve-Lod", "[char]::IsDigit",
            "MinimumVertices", "VertexBudgets", "MinimumTriangles", "TriangleBudgets",
            "$Label Unity metadata",
            "Assert-TrackedFileWithMeta $mtlPath 'Hero MTL dependency'",
            "Assert-TrackedFileWithMeta $texturePath 'Hero texture dependency'",
            "READY_FOR_LICENSED_UNITY_IMPORT", "UNITY_INSPECTION_REQUIRED",
            "mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)
        self.assertNotIn("python", source.lower())

    def test_native_valid_obj_with_nested_dependencies_passes(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for native PowerShell behavior coverage")
        root = HELPER.make_repo()
        try:
            source = (HELPER.HERO_ROOT / "AfareetKing_Production.obj").as_posix()
            result = self.run_command([pwsh, "-NoProfile", "-File", str(NATIVE), "-RepoRoot", str(root), "-Source", source])
            self.assertEqual(result.returncode, 0, result.stdout)
            self.assertIn("AFAREET_UART003_HERO_NATIVE_PREFLIGHT_OK", result.stdout)
            self.assertIn("verdict=READY_FOR_LICENSED_UNITY_IMPORT", result.stdout)
            self.assertIn("dependenciesTracked=true", result.stdout)
            self.assertIn("dependenciesPackageLocal=true", result.stdout)
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_native_missing_texture_meta_is_blocked(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for native PowerShell behavior coverage")
        root = HELPER.make_repo()
        try:
            (root / HELPER.HERO_ROOT / "textures/hero.png.meta").unlink()
            source = (HELPER.HERO_ROOT / "AfareetKing_Production.obj").as_posix()
            result = self.run_command([pwsh, "-NoProfile", "-File", str(NATIVE), "-RepoRoot", str(root), "-Source", source])
            self.assertNotEqual(result.returncode, 0, result.stdout)
            self.assertIn("AFAREET_UART003_HERO_NATIVE_PREFLIGHT_ERROR", result.stdout)
            self.assertIn("Hero texture dependency", result.stdout)
            self.assertIn("Unity metadata is missing", result.stdout)
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_native_opaque_fbx_stays_unity_inspection_required(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for native PowerShell behavior coverage")
        root = HELPER.make_repo(obj=False)
        try:
            source = (HELPER.HERO_ROOT / "AfareetKing_Production.fbx").as_posix()
            result = self.run_command([pwsh, "-NoProfile", "-File", str(NATIVE), "-RepoRoot", str(root), "-Source", source])
            self.assertEqual(result.returncode, 0, result.stdout)
            self.assertIn("verdict=UNITY_INSPECTION_REQUIRED", result.stdout)
            self.assertIn("unityInspectionRequired=true", result.stdout)
            self.assertIn("verified=false", result.stdout)
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_native_and_staging_scripts_parse_with_pwsh(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for PowerShell parser coverage")
        for script in (NATIVE, STAGING):
            command = (
                "$tokens=$null;$errors=$null;"
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
                "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Host $_.Message};exit 1}"
            )
            result = self.run_command([pwsh, "-NoProfile", "-Command", command])
            self.assertEqual(result.returncode, 0, f"{script}: {result.stdout}")


if __name__ == "__main__":
    unittest.main()
