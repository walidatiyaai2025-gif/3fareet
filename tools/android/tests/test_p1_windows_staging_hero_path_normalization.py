import shutil
import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools/android/stage_production_candidate_windows.ps1"


class WindowsStagingHeroPathNormalizationTests(unittest.TestCase):
    @staticmethod
    def _run(command):
        return subprocess.run(
            command,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def _normalize_with_native_function(self, value: str) -> str:
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for native PowerShell normalization coverage")
        escaped_script = SCRIPT.as_posix().replace("'", "''")
        escaped_value = value.replace("'", "''")
        command = (
            "$tokens=$null;$errors=$null;"
            f"$ast=[System.Management.Automation.Language.Parser]::ParseFile('{escaped_script}',[ref]$tokens,[ref]$errors);"
            "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Host $_.Message};exit 2};"
            "$fn=$ast.Find({param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] "
            "-and $node.Name -eq 'Normalize-HeroSource'},$true);"
            "if($null -eq $fn){Write-Host 'Normalize-HeroSource function missing';exit 3};"
            "Invoke-Expression $fn.Extent.Text;"
            f"Normalize-HeroSource '{escaped_value}'"
        )
        result = self._run([pwsh, "-NoProfile", "-Command", command])
        self.assertEqual(result.returncode, 0, result.stdout)
        return result.stdout.strip()

    def test_native_normalizer_accepts_readiness_equivalent_forms(self):
        canonical = "Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
        cases = (
            canonical,
            "./" + canonical,
            "././" + canonical,
            "unity_game/" + canonical,
            "./unity_game/" + canonical,
            ".\\unity_game\\" + canonical.replace("/", "\\"),
        )
        for source in cases:
            with self.subTest(source=source):
                self.assertEqual(canonical, self._normalize_with_native_function(source))

    def test_native_normalizer_does_not_collapse_parent_traversal(self):
        value = "../Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
        self.assertEqual(value, self._normalize_with_native_function(value))

    def test_staging_applies_normalization_before_unity_assets_gate_and_keeps_ps5_syntax(self):
        source = SCRIPT.read_text(encoding="utf-8")
        for required in (
            "function Normalize-HeroSource",
            "$HeroSource = Normalize-HeroSource $HeroSource",
            "unity_game/Assets/",
            "HeroSource must normalize to a Unity Assets/ path",
            "AFAREET_STAGING_HERO_HANDOFF_PREFLIGHT_START",
            "mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)
        self.assertLess(
            source.index("$HeroSource = Normalize-HeroSource $HeroSource"),
            source.index("HeroSource must normalize to a Unity Assets/ path"),
        )
        self.assertNotIn("??", source)

    def test_staging_script_parses_with_pwsh(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for PowerShell parser coverage")
        command = (
            "$tokens=$null;$errors=$null;"
            f"[System.Management.Automation.Language.Parser]::ParseFile('{SCRIPT.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
            "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Host $_.Message};exit 1}"
        )
        result = self._run([pwsh, "-NoProfile", "-Command", command])
        self.assertEqual(result.returncode, 0, result.stdout)


if __name__ == "__main__":
    unittest.main()
