import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
NATIVE_PREFLIGHT = REPO_ROOT / "tools/android/rival_production_handoff_preflight_windows.ps1"
STAGING_WRAPPER = REPO_ROOT / "tools/android/stage_production_candidate_windows.ps1"
RIVAL_FILES = (
    "Rival_01_WedgeCoupe_Production.obj",
    "Rival_02_FastbackMuscle_Production.obj",
    "Rival_03_CompactPrototype_Production.obj",
)


class WindowsRivalDependencyPreflightTests(unittest.TestCase):
    @staticmethod
    def _run(command, cwd=None):
        return subprocess.run(
            command,
            cwd=cwd,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def _init_fixture(
        self,
        root: Path,
        *,
        escaping_texture=False,
        omit_texture_meta=False,
        wavefront_grammar=False,
    ):
        git = shutil.which("git")
        self.assertIsNotNone(git)
        package = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production"
        materials = package / "materials"
        textures = package / "textures"
        materials.mkdir(parents=True)
        textures.mkdir(parents=True)

        for index, rival_file in enumerate(RIVAL_FILES, start=1):
            obj = package / rival_file
            complex_grammar = wavefront_grammar and index == 1
            if complex_grammar:
                mtllib_line = 'mtllib "materials/rival 1 primary.mtl" "materials/rival 1 secondary.mtl" # exported by DCC'
            else:
                mtllib_line = f"mtllib materials/rival_{index}.mtl"
            obj.write_text(
                "\n".join(
                    (
                        mtllib_line,
                        "o Test_LOD0",
                        "usemtl Mat_LOD0",
                        "o Test_LOD1",
                        "usemtl Mat_LOD1",
                        "o Test_LOD2",
                        "usemtl Mat_LOD2",
                    )
                ) + "\n",
                encoding="utf-8",
            )
            Path(str(obj) + ".meta").write_text(f"guid: obj{index}\n", encoding="utf-8")

            if escaping_texture and index == 1:
                outside = root / "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/outside.png"
                outside.write_bytes(b"outside")
                Path(str(outside) + ".meta").write_text("guid: outside\n", encoding="utf-8")
                texture_ref = "../../outside.png"
            elif complex_grammar:
                texture = textures / "rival 1 base.png"
                texture.write_bytes(b"texture-1-spaced")
                Path(str(texture) + ".meta").write_text("guid: texture1spaced\n", encoding="utf-8")
                texture_ref = '"../textures/rival 1 base.png"'
            else:
                texture = textures / f"rival_{index}.png"
                texture.write_bytes(f"texture-{index}".encode("ascii"))
                if not (omit_texture_meta and index == 1):
                    Path(str(texture) + ".meta").write_text(f"guid: texture{index}\n", encoding="utf-8")
                texture_ref = f"../textures/rival_{index}.png"

            if complex_grammar:
                primary = materials / "rival 1 primary.mtl"
                primary.write_text(
                    "\n".join((
                        "newmtl Mat_LOD0",
                        "Kd 1 1 1",
                        f"map_Kd -s 1 1 1 {texture_ref} # exporter options",
                        "newmtl Mat_LOD1",
                        "Kd 1 1 1",
                        f"map_Kd -o 0 0 0 {texture_ref}",
                    )) + "\n",
                    encoding="utf-8",
                )
                Path(str(primary) + ".meta").write_text("guid: mtl1primary\n", encoding="utf-8")
                secondary = materials / "rival 1 secondary.mtl"
                secondary.write_text(
                    "\n".join((
                        "newmtl Mat_LOD2",
                        "Kd 1 1 1",
                        f"map_Kd -clamp on {texture_ref} # shared texture",
                    )) + "\n",
                    encoding="utf-8",
                )
                Path(str(secondary) + ".meta").write_text("guid: mtl1secondary\n", encoding="utf-8")
            else:
                mtl = materials / f"rival_{index}.mtl"
                mtl.write_text(
                    "\n".join(
                        line
                        for lod in range(3)
                        for line in (f"newmtl Mat_LOD{lod}", "Kd 1 1 1", f"map_Kd {texture_ref}")
                    ) + "\n",
                    encoding="utf-8",
                )
                Path(str(mtl) + ".meta").write_text(f"guid: mtl{index}\n", encoding="utf-8")

        for args in (
            [git, "init"],
            [git, "config", "user.email", "ci@example.invalid"],
            [git, "config", "user.name", "CI Fixture"],
            [git, "add", "."],
            [git, "commit", "-m", "fixture"],
        ):
            result = self._run(args, cwd=root)
            self.assertEqual(result.returncode, 0, result.stdout)

    def test_stage_wrapper_runs_native_dependency_gate_before_unity(self):
        source = STAGING_WRAPPER.read_text(encoding="utf-8")
        for required in (
            "rival_production_handoff_preflight_windows.ps1",
            "AFAREET_STAGING_RIVAL_DEPENDENCY_PREFLIGHT_START",
            "AFAREET_STAGING_RIVAL_DEPENDENCY_PREFLIGHT_OK",
            "dependenciesTracked=true",
            "dependenciesPackageLocal=true",
            "mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)
        self.assertLess(
            source.index("AFAREET_STAGING_RIVAL_DEPENDENCY_PREFLIGHT_START"),
            source.index("Start-Process -FilePath $UnityPath"),
        )
        self.assertNotIn("python", source.lower())

    def test_native_contract_tracks_obj_mtl_texture_and_meta_without_python(self):
        source = NATIVE_PREFLIGHT.read_text(encoding="utf-8")
        for required in (
            "ls-files --error-unmatch",
            "Rival_01_WedgeCoupe_Production.obj",
            "Rival_02_FastbackMuscle_Production.obj",
            "Rival_03_CompactPrototype_Production.obj",
            "Rival MTL Unity metadata",
            "Rival texture Unity metadata",
            "Resolve-PackageDependency $packageRoot (Split-Path -Parent $mtlPath)",
            "material is not texture-mapped by a supplied package-local MTL",
            "Split-WavefrontArguments",
            "unterminated quoted argument",
            "AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_OK",
            "mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)
        self.assertNotIn("python", source.lower())

    def test_native_preflight_accepts_nested_mtl_texture_layout(self):
        pwsh = shutil.which("pwsh")
        git = shutil.which("git")
        if not pwsh or not git:
            self.skipTest("pwsh and git are required for native PowerShell behavior coverage")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._init_fixture(root)
            result = self._run([pwsh, "-NoProfile", "-File", str(NATIVE_PREFLIGHT), "-RepoRoot", str(root)])
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_OK", result.stdout)
        self.assertIn("dependenciesTracked=true", result.stdout)
        self.assertIn("dependenciesPackageLocal=true", result.stdout)

    def test_native_preflight_accepts_multiple_quoted_mtllibs_and_optioned_spaced_texture_paths(self):
        pwsh = shutil.which("pwsh")
        git = shutil.which("git")
        if not pwsh or not git:
            self.skipTest("pwsh and git are required for native PowerShell behavior coverage")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._init_fixture(root, wavefront_grammar=True)
            result = self._run([pwsh, "-NoProfile", "-File", str(NATIVE_PREFLIGHT), "-RepoRoot", str(root)])
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_OK", result.stdout)
        self.assertIn("mtllibs=4", result.stdout)
        self.assertIn("textures=3", result.stdout)
        self.assertIn("dependenciesTracked=true", result.stdout)
        self.assertIn("dependenciesPackageLocal=true", result.stdout)
        self.assertIn("verified=false", result.stdout)

    def test_native_preflight_rejects_texture_escape_from_nested_mtl(self):
        pwsh = shutil.which("pwsh")
        git = shutil.which("git")
        if not pwsh or not git:
            self.skipTest("pwsh and git are required for native PowerShell behavior coverage")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._init_fixture(root, escaping_texture=True)
            result = self._run([pwsh, "-NoProfile", "-File", str(NATIVE_PREFLIGHT), "-RepoRoot", str(root)])
        self.assertNotEqual(result.returncode, 0, result.stdout)
        self.assertIn("escapes the Rival handoff package", result.stdout)

    def test_native_preflight_rejects_missing_texture_meta(self):
        pwsh = shutil.which("pwsh")
        git = shutil.which("git")
        if not pwsh or not git:
            self.skipTest("pwsh and git are required for native PowerShell behavior coverage")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._init_fixture(root, omit_texture_meta=True)
            result = self._run([pwsh, "-NoProfile", "-File", str(NATIVE_PREFLIGHT), "-RepoRoot", str(root)])
        self.assertNotEqual(result.returncode, 0, result.stdout)
        for expected in (
            "AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_ERROR",
            "Rival texture Unity",
            "metadata is missing:",
            "rival_1.png.meta",
        ):
            self.assertIn(expected, result.stdout)

    def test_native_and_staging_scripts_parse_with_pwsh(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is required for PowerShell parser coverage")
        for script in (NATIVE_PREFLIGHT, STAGING_WRAPPER):
            command = (
                "$tokens=$null;$errors=$null;"
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
                "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Host $_.Message};exit 1}"
            )
            result = self._run([pwsh, "-NoProfile", "-Command", command])
            self.assertEqual(result.returncode, 0, f"{script}: {result.stdout}")


if __name__ == "__main__":
    unittest.main()
