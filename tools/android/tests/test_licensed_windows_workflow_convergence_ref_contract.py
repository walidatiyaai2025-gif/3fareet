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

        lines = text.splitlines()
        options_index = next(i for i, line in enumerate(lines) if line.strip() == "options:")
        expected_index = next(i for i, line in enumerate(lines) if line.strip() == "expected_sha:")
        option_values = [
            line.strip()[2:].strip().strip("'\"")
            for line in lines[options_index + 1 : expected_index]
            if line.strip().startswith("- ")
        ]
        self.assertEqual(
            ["agent/p1-remediation-convergence", "agent/unblock-final-5"],
            option_values,
        )

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

    def test_experimental_mode_is_explicit_and_non_release(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        for required in (
            "candidate_mode:",
            "default: 'production'",
            "- 'production'",
            "- 'experimental'",
            "build_experimental_windows.ps1",
            "inputs.candidate_mode == 'experimental'",
            "artifactClass",
            "releaseEvidenceEligible must remain JSON boolean false for experimental APKs.",
            "physicalDeviceVerified must remain JSON boolean false until device evidence exists.",
            "artifacts\\android-experimental",
        ):
            self.assertIn(required, text)

        self.assertIn("inputs.candidate_mode == 'production'", text)
        self.assertIn("run_local_candidate_windows.ps1", text)

    def test_documentation_explains_premerge_licensed_proof(self):
        text = DOC.read_text(encoding="utf-8")
        self.assertIn("agent/p1-remediation-convergence", text)
        self.assertIn("agent/unblock-final-5", text)
        self.assertIn("before merging", text.lower())
        self.assertIn("expected_sha", text)
        self.assertIn("allowlist", text.lower())


if __name__ == "__main__":
    unittest.main()
