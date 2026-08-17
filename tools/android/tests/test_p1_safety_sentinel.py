import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]
ROOT = Path(__file__).resolve().parents[3]


def load_module():
    path = TOOLS / "p1_safety_sentinel.py"
    spec = importlib.util.spec_from_file_location("p1_safety_sentinel_under_test", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


MODULE = load_module()


class P1SafetySentinelTests(unittest.TestCase):
    def scan_text(self, suffix: str, content: str):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / f"sample{suffix}"
            path.write_text(content, encoding="utf-8")
            return MODULE.scan_file(path, path.name)

    def test_current_authoritative_repo_scope_is_clean_and_includes_unity_handoff(self):
        paths, violations = MODULE.scan_repo(ROOT, TOOLS / "p1_operator_release_chain.json")
        self.assertEqual([], violations)
        self.assertIn("tools/android/verify_p1_release_publication.py", paths)
        self.assertIn("tools/android/run_p1_licensed_staging_windows.ps1", paths)
        self.assertIn("unity_game/Assets/Afareet/Editor/P1ProductionCandidateStagingHandoff.cs", paths)
        self.assertNotIn("tools/android/tests/test_p1_safety_sentinel.py", paths)
        self.assertGreaterEqual(len(paths), 13)

        # The authoritative wrapper deliberately delegates to this lower-level staging
        # implementation. Keep it under an explicit safety scan even though the chain's
        # stage tool now points at the wrapper.
        low_level = TOOLS / "stage_production_candidate_windows.ps1"
        low_level_violations = MODULE.scan_file(
            low_level,
            "tools/android/stage_production_candidate_windows.ps1",
        )
        self.assertEqual([], low_level_violations)

        native_packet_verifier = TOOLS / "verify_p1_licensed_handoff_packet_windows.ps1"
        verifier_violations = MODULE.scan_file(
            native_packet_verifier,
            "tools/android/verify_p1_licensed_handoff_packet_windows.ps1",
        )
        self.assertEqual([], verifier_violations)

    def test_python_detects_protected_true_in_dict_assignment_and_keyword(self):
        violations = self.scan_text(
            ".py",
            """
result = {"verified": True, "runtimeVerified": False}
publicationEligible = True
emit(ownerAccepted=True)
""",
        )
        self.assertEqual(3, len(violations))
        self.assertTrue(all(v.rule == "P1_SELF_PROMOTION" for v in violations))

    def test_python_detects_executed_git_push_but_ignores_explanatory_prose(self):
        bad = self.scan_text(
            ".py",
            "import subprocess\nsubprocess.run(['git', 'push', 'origin', 'main'], check=True)\n",
        )
        self.assertTrue(any(v.rule == "P1_AUTOMATED_PUBLICATION" for v in bad))

        safe = self.scan_text(
            ".py",
            "note = 'Never run git push or set verified=True from this preflight.'\nprint(note)\n",
        )
        self.assertEqual([], safe)

    def test_powershell_detects_self_promotion_and_remote_mutation(self):
        violations = self.scan_text(
            ".ps1",
            """
$report = [pscustomobject]@{
  verified = $true
  publicationEligible = $false
}
& $git.Source -C $RepoRoot push origin main
""",
        )
        self.assertTrue(any(v.rule == "P1_SELF_PROMOTION" for v in violations))
        self.assertTrue(any(v.rule == "P1_AUTOMATED_PUBLICATION" for v in violations))

    def test_powershell_ignores_comments_and_quoted_safety_messages(self):
        violations = self.scan_text(
            ".ps1",
            """
# verified = $true
Write-Host 'Never set publicationPerformed = $true or run git tag here.'
$report = [pscustomobject]@{ verified = $false; publicationPerformed = $false }
""",
        )
        self.assertEqual([], violations)

    def test_csharp_detects_protected_true_and_process_release_action(self):
        violations = self.scan_text(
            ".cs",
            """
using System.Diagnostics;
class Gate {
  void Run() {
    report.ownerAccepted = true;
    Process.Start("gh", "release create v1.0");
  }
}
""",
        )
        self.assertTrue(any(v.rule == "P1_SELF_PROMOTION" for v in violations))
        self.assertTrue(any(v.rule == "P1_AUTOMATED_PUBLICATION" for v in violations))

    def test_csharp_ignores_comments_and_string_prose(self):
        violations = self.scan_text(
            ".cs",
            """
class Gate {
  // verified = true;
  string note = "publicationEligible = true is forbidden";
  bool verified = false;
}
""",
        )
        self.assertEqual([], violations)

    def test_chain_scope_fails_closed_on_missing_or_escaping_tool(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = root / "chain.json"
            chain.write_text(
                json.dumps(
                    {
                        "state": "P1_AUTHORITATIVE_OPERATOR_CHAIN",
                        "authoritativeForP1": True,
                        "orderedStages": [{"tool": "../escape.py"}],
                    }
                ),
                encoding="utf-8",
            )
            with self.assertRaisesRegex(ValueError, "escapes repo root"):
                MODULE.load_scan_paths(root, chain)


if __name__ == "__main__":
    unittest.main()
