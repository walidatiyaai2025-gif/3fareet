import hashlib
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools/android/verify_hero_refinement_handoff_windows.ps1"


class VerifyHeroRefinementHandoffWindowsTests(unittest.TestCase):
    @staticmethod
    def record(path: Path, role: str):
        payload = path.read_bytes()
        return {
            "fileName": path.name,
            "sizeBytes": len(payload),
            "sha256": hashlib.sha256(payload).hexdigest(),
            "role": role,
        }

    def fixture(self, root: Path):
        fbx = root / "AfareetKing_Hero.fbx"
        glb = root / "AfareetKing_Hero.glb"
        blend = root / "AfareetKing_Hero.blend"
        fbx.write_bytes(b"native-fbx-fixture")
        glb.write_bytes(b"native-glb-fixture")
        blend.write_bytes(b"native-blend-fixture")

        fbx_record = self.record(fbx, "UNITY_REFINEMENT_INTAKE")
        receipt = {
            "schemaVersion": 1,
            "task": "UART-003",
            "classification": "REFINEMENT_CANDIDATE",
            "origin": "EXTERNAL_USER_HANDOFF",
            "files": {
                "fbx": fbx_record,
                "glb": self.record(glb, "INSPECTION_COMPANION"),
                "blend": self.record(blend, "DCC_SOURCE_COMPANION"),
            },
            "productionGate": False,
            "visualAcceptance": False,
            "ownerApproval": False,
            "verified": False,
            "inspectionBoundary": "BYTE_IDENTITY_ONLY_LICENSED_UNITY_INSPECTION_REQUIRED",
        }
        manifest = {
            "schemaVersion": 1,
            "classification": "REFINEMENT_CANDIDATE",
            "sourceFileName": fbx_record["fileName"],
            "sha256": fbx_record["sha256"],
            "sizeBytes": fbx_record["sizeBytes"],
            "productionGate": False,
            "visualAcceptance": False,
        }
        receipt_path = root / "receipt.json"
        manifest_path = root / "manifest.json"
        receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        return fbx, glb, blend, receipt_path, manifest_path, receipt

    def run_native(self, *, fbx, glb, blend, receipt, manifest, output=None):
        pwsh = shutil.which("pwsh")
        if pwsh is None:
            self.skipTest("pwsh is unavailable on this host")
        command = [
            pwsh,
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            str(SCRIPT),
            "-Fbx",
            str(fbx),
            "-Glb",
            str(glb),
            "-Blend",
            str(blend),
            "-Receipt",
            str(receipt),
            "-RefinementManifest",
            str(manifest),
        ]
        if output is not None:
            command.extend(["-Output", str(output)])
        return subprocess.run(command, text=True, capture_output=True, check=False)

    def test_native_script_contains_fail_closed_nonproduction_contract(self):
        source = SCRIPT.read_text(encoding="utf-8")
        for required in (
            "REFINEMENT_HANDOFF_MATCH_NOT_PRODUCTION",
            "EXTERNAL_USER_HANDOFF",
            "BYTE_IDENTITY_ONLY_LICENSED_UNITY_INSPECTION_REQUIRED",
            "Get-FileHash -LiteralPath $resolved -Algorithm SHA256",
            "receipt FBX SHA-256 must match hero_refinement_candidate_manifest.json",
            "Require-False $receiptObject $key 'receipt'",
            "productionGate = $false",
            "visualAcceptance = $false",
            "ownerApproval = $false",
            "verified = $false",
        ):
            self.assertIn(required, source)
        for forbidden in (
            "productionGate = $true",
            "visualAcceptance = $true",
            "ownerApproval = $true",
            "verified = $true",
        ):
            self.assertNotIn(forbidden, source)

    def test_native_script_parses_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh")
        if pwsh is None:
            self.skipTest("pwsh is unavailable on this host")
        escaped_script = str(SCRIPT).replace("'", "''")
        command = (
            "$tokens=$null;$errors=$null;"
            f"[System.Management.Automation.Language.Parser]::ParseFile('{escaped_script}',"
            "[ref]$tokens,[ref]$errors)|Out-Null;"
            "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Error $_.Message};exit 1}"
        )
        completed = subprocess.run(
            [pwsh, "-NoProfile", "-Command", command],
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)

    def test_native_verifier_accepts_exact_fixture_and_writes_nonverified_result(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fbx, glb, blend, receipt, manifest, _ = self.fixture(root)
            output = root / "result.json"
            completed = self.run_native(
                fbx=fbx,
                glb=glb,
                blend=blend,
                receipt=receipt,
                manifest=manifest,
                output=output,
            )
            self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
            self.assertIn("AFAREET_HERO_REFINEMENT_HANDOFF_NATIVE_OK", completed.stdout)
            result = json.loads(output.read_text(encoding="utf-8-sig"))
            self.assertEqual(result["verdict"], "REFINEMENT_HANDOFF_MATCH_NOT_PRODUCTION")
            self.assertTrue(result["handoffByteIdentityMatch"])
            self.assertFalse(result["productionGate"])
            self.assertFalse(result["visualAcceptance"])
            self.assertFalse(result["ownerApproval"])
            self.assertFalse(result["verified"])

    def test_native_verifier_rejects_receipt_that_promotes_candidate(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fbx, glb, blend, receipt_path, manifest, receipt = self.fixture(root)
            receipt["productionGate"] = True
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            completed = self.run_native(
                fbx=fbx,
                glb=glb,
                blend=blend,
                receipt=receipt_path,
                manifest=manifest,
            )
            self.assertNotEqual(completed.returncode, 0)
            self.assertIn("must be JSON boolean false", completed.stdout + completed.stderr)

    def test_native_verifier_rejects_byte_drift(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fbx, glb, blend, receipt, manifest, _ = self.fixture(root)
            glb.write_bytes(b"drifted")
            completed = self.run_native(
                fbx=fbx,
                glb=glb,
                blend=blend,
                receipt=receipt,
                manifest=manifest,
            )
            self.assertNotEqual(completed.returncode, 0)
            combined = completed.stdout + completed.stderr
            self.assertTrue("GLB size mismatch" in combined or "GLB SHA-256 mismatch" in combined)


if __name__ == "__main__":
    unittest.main()
