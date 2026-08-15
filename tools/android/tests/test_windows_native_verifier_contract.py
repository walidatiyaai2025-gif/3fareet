from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "android" / "run_local_candidate_windows.ps1"


class WindowsNativeVerifierContractTests(unittest.TestCase):
    def test_candidate_orchestrator_uses_native_powershell_verifiers(self):
        text = SCRIPT.read_text(encoding="utf-8")
        required = [
            "verify_unity_text_normalization_windows.ps1",
            "verify_unity_package_lock_windows.ps1",
            "verify_local_candidate_windows.ps1",
            "AFAREET_WINDOWS_NATIVE_VERIFIERS_OK pythonRequired=False",
            "& $textNormalizeScript -RepoRoot $RepoRoot",
            "& $packageVerifyScript -RepoRoot $RepoRoot -ManifestPath $packageManifest -LockPath $packageLock",
            "-TestMetadata $testMetadata",
            "-BuildMetadata $buildMetadata",
            "-Apk $apk",
            "-Output $manifest",
        ]
        for marker in required:
            self.assertIn(marker, text, marker)

        forbidden = [
            "Resolve-Python3",
            "Test-Python3Candidate",
            "AFAREET_PYTHON_RESOLVED",
            "verify_local_candidate.py",
            "verify_unity_package_lock.py",
            "verify_unity_text_normalization.py",
        ]
        for marker in forbidden:
            self.assertNotIn(marker, text, marker)

    def test_initial_dirty_evidence_is_preserved_before_native_preflights(self):
        text = SCRIPT.read_text(encoding="utf-8")
        preserve = text.index(
            'Preserve-DirtyTreeEvidence -Phase "INITIAL_TREE" -Changes $initialDirty'
        )
        refusal = text.index(
            "Candidate orchestration requires a clean Git working tree before Unity starts."
        )
        stale_purge = text.index("Clear-StaleCandidateEvidence\n")
        native_marker = text.index("AFAREET_WINDOWS_NATIVE_VERIFIERS_OK")
        text_preflight = text.index("AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START")

        self.assertLess(preserve, refusal)
        self.assertLess(refusal, stale_purge)
        self.assertLess(stale_purge, native_marker)
        self.assertLess(native_marker, text_preflight)

    def test_native_verifier_files_exist(self):
        for name in (
            "verify_unity_text_normalization_windows.ps1",
            "verify_unity_package_lock_windows.ps1",
            "verify_local_candidate_windows.ps1",
        ):
            self.assertTrue((ROOT / "android" / name).is_file(), name)


if __name__ == "__main__":
    unittest.main()
