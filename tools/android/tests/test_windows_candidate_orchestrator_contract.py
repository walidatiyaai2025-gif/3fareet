import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "android" / "run_local_candidate_windows.ps1"


class WindowsCandidateOrchestratorContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.text = SCRIPT.read_text(encoding="utf-8")

    def test_dirty_tree_patch_capture_keeps_git_stderr_separate(self):
        required = (
            "$stderrPath = Join-Path $evidenceDir \"git-dirty-$phaseKey.stderr.txt\"",
            "Start-Process",
            "-RedirectStandardOutput $patchPath",
            "-RedirectStandardError $stderrPath",
            "$patchExitCode = $gitProcess.ExitCode",
            "'diff', '--binary', 'HEAD', '--'",
            "AFAREET_DIRTY_TREE_EVIDENCE",
        )
        for marker in required:
            with self.subTest(marker=marker):
                self.assertIn(marker, self.text)

        self.assertNotIn("diff --binary HEAD -- 2>&1", self.text)

    def test_initial_dirty_tree_is_preserved_before_cleanup_and_refusal(self):
        required = (
            '$initialDirty = @(& git -C $RepoRoot status --porcelain 2>$null)',
            'Preserve-DirtyTreeEvidence -Phase "INITIAL_TREE" -Changes $initialDirty',
            'INITIAL_TREE_DIRTY',
            'Initial dirty-tree status/patch/stderr evidence was preserved',
            'Clear-StaleCandidateEvidence',
        )
        for marker in required:
            with self.subTest(marker=marker):
                self.assertIn(marker, self.text)

        preserve = self.text.index(
            'Preserve-DirtyTreeEvidence -Phase "INITIAL_TREE" -Changes $initialDirty'
        )
        refusal = self.text.index(
            'Candidate orchestration requires a clean Git working tree before Unity starts.'
        )
        cleanup_call = self.text.index("Clear-StaleCandidateEvidence\n")
        self.assertLess(preserve, refusal)
        self.assertLess(refusal, cleanup_call)

    def test_package_preflight_happens_before_unity_execution(self):
        required = (
            "verify_unity_package_lock.py",
            "AFAREET_PACKAGE_PREFLIGHT_START",
            "AFAREET_PACKAGE_PREFLIGHT_OK",
            'Assert-CleanTree "PACKAGE_PREFLIGHT"',
            '& $testScript @sharedParams',
            '& $buildScript @sharedParams',
        )
        for marker in required:
            with self.subTest(marker=marker):
                self.assertIn(marker, self.text)

        preflight_ok = self.text.index("AFAREET_PACKAGE_PREFLIGHT_OK")
        test_start = self.text.index("& $testScript @sharedParams")
        build_start = self.text.index("& $buildScript @sharedParams")
        self.assertLess(preflight_ok, test_start)
        self.assertLess(preflight_ok, build_start)


if __name__ == "__main__":
    unittest.main()
