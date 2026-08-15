import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
WORKFLOW = REPO / ".github" / "workflows" / "unity-licensed-windows-candidate.yml"
DOC = REPO / "docs" / "qa" / "LICENSED_WINDOWS_GITHUB_RUNNER.md"


class LicensedWindowsWorkflowConvergenceRefContractTests(unittest.TestCase):
    def test_workflow_allows_only_convergence_or_canonical_ref(self):
        text = WORKFLOW.read_text(encoding="utf-8")

        for required in (
            "candidate_ref:",
            "type: choice",
            "default: 'agent/p1-remediation-convergence'",
            "- 'agent/p1-remediation-convergence'",
            "- 'agent/unblock-final-5'",
            "Validate requested candidate ref before checkout",
            "$allowedRefs = @(",
            "candidate_ref is not in the production allowlist",
            "ref: ${{ steps.scope.outputs.candidate_ref }}",
            "EXPECTED_SHA: ${{ inputs.expected_sha }}",
            "Candidate ref moved or wrong SHA was requested.",
            "persist-credentials: false",
            "run_local_candidate_windows.ps1",
        ):
            self.assertIn(required, text)

        self.assertNotIn("ref: agent/unblock-final-5", text)
        self.assertNotIn("ref: ${{ inputs.candidate_ref }}", text)
        self.assertEqual(2, text.count("'agent/p1-remediation-convergence'"))
        self.assertEqual(2, text.count("'agent/unblock-final-5'"))

    def test_ref_allowlist_guard_occurs_before_checkout(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        guard = text.index("- name: Validate requested candidate ref before checkout")
        checkout = text.index("- name: Checkout selected production candidate ref")
        self.assertLess(guard, checkout)
        self.assertIn("candidate_ref=$candidateRef", text[guard:checkout])

    def test_expected_sha_has_no_hardcoded_commit_default(self):
        lines = WORKFLOW.read_text(encoding="utf-8").splitlines()
        expected_index = next(i for i, line in enumerate(lines) if line.strip() == "expected_sha:")
        expected_block = "\n".join(lines[expected_index : expected_index + 6])
        self.assertNotRegex(expected_block, r"default:\s*['\"]?[0-9a-fA-F]{40}")
        self.assertIn("required: true", expected_block)

    def test_documentation_explains_premerge_licensed_proof(self):
        text = DOC.read_text(encoding="utf-8")
        self.assertIn("agent/p1-remediation-convergence", text)
        self.assertIn("agent/unblock-final-5", text)
        self.assertIn("before merging", text.lower())
        self.assertIn("expected_sha", text)
        self.assertIn("allowlist", text.lower())


if __name__ == "__main__":
    unittest.main()
