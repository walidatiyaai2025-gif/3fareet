import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT / "tools" / "android"
EDITOR = ROOT / "unity_game" / "Assets" / "Afareet" / "Editor"


class P1StagingAuthorizationLineageContractTests(unittest.TestCase):
    def test_authoritative_wrapper_hashes_native_authorization_and_forwards_three_fingerprints(self):
        text = (TOOLS / "run_p1_licensed_staging_windows.ps1").read_text(encoding="utf-8")
        required = (
            "verification.packetSha256",
            "verification.operatorChainSha256",
            "Get-FileHash -Algorithm SHA256 -LiteralPath $verifyOutput",
            "HandoffPacketSha256 = $handoffPacketSha256",
            "NativeHandoffVerificationSha256 = $nativeVerificationSha256",
            "OperatorChainSha256 = $operatorChainSha256",
        )
        for marker in required:
            self.assertIn(marker, text)
        self.assertLess(text.index("& $verifyScript"), text.index("& $stageScript"))

    def test_low_level_staging_requires_authorization_and_binds_it_to_unity_report_v3(self):
        text = (TOOLS / "stage_production_candidate_windows.ps1").read_text(encoding="utf-8")
        for marker in (
            "[Parameter(Mandatory = $true)]\n    [string]$HandoffPacketSha256",
            "[Parameter(Mandatory = $true)]\n    [string]$NativeHandoffVerificationSha256",
            "[Parameter(Mandatory = $true)]\n    [string]$OperatorChainSha256",
            "-afareetHandoffPacketSha256",
            "-afareetNativeHandoffVerificationSha256",
            "-afareetOperatorChainSha256",
            "schemaVersion -ne 3",
            "handoffReport.handoffPacketSha256",
            "handoffReport.nativeHandoffVerificationSha256",
            "handoffReport.operatorChainSha256",
            "handoffReport.authorizationSourceGitSha",
        ):
            self.assertIn(marker, text)

    def test_unity_report_v3_requires_authorization_arguments_and_never_self_approves(self):
        text = (EDITOR / "P1ProductionCandidateStagingHandoff.cs").read_text(encoding="utf-8")
        for marker in (
            'HandoffPacketSha256Argument = "-afareetHandoffPacketSha256"',
            'NativeHandoffVerificationSha256Argument = "-afareetNativeHandoffVerificationSha256"',
            'OperatorChainSha256Argument = "-afareetOperatorChainSha256"',
            "public int schemaVersion = 3",
            "public string authorizationSourceGitSha",
            "public string handoffPacketSha256",
            "public string nativeHandoffVerificationSha256",
            "public string operatorChainSha256",
            "authorizationSourceGitSha = gitSha",
        ):
            self.assertIn(marker, text)
        for forbidden in (
            "public bool verified = true",
            "public bool runtimeVerified = true",
            "public bool ownerAccepted = true",
            "public bool publicationEligible = true",
            "public bool candidateBuildStarted = true",
        ):
            self.assertNotIn(forbidden, text)

    def test_lineage_requires_v3_and_carries_same_authorization_block(self):
        text = (TOOLS / "verify_p1_staging_lineage_windows.ps1").read_text(encoding="utf-8")
        for marker in (
            "schema must be 3",
            "authorizationSourceGitSha",
            "handoffPacketSha256",
            "nativeHandoffVerificationSha256",
            "operatorChainSha256",
            "stagingReportSchemaVersion = 3",
            "stagingAuthorization = [ordered]@{",
        ):
            self.assertIn(marker, text)

    def test_staged_candidate_envelope_preserves_authorization_before_candidate_build(self):
        text = (TOOLS / "run_p1_staged_candidate_windows.ps1").read_text(encoding="utf-8")
        for marker in (
            "stagingReportSchemaVersion -ne 3",
            "$authorization = $lineage.stagingAuthorization",
            "handoffPacketSha256",
            "nativeHandoffVerificationSha256",
            "operatorChainSha256",
            "stagingAuthorization = [ordered]@{",
            "schemaVersion = 3",
        ):
            self.assertIn(marker, text)
        self.assertLess(text.index("& $lineageVerifier"), text.index("& $genericRunner"))

    def test_device_precheck_semantically_cross_checks_authorization_before_adb(self):
        text = (TOOLS / "prepare_p1_candidate_device.py").read_text(encoding="utf-8")
        for marker in (
            'def _authorization(',
            '"authorizationSourceGitSha"',
            '"handoffPacketSha256"',
            '"nativeHandoffVerificationSha256"',
            '"operatorChainSha256"',
            'staging_record.get("schemaVersion") != 3',
            'staging.get("schemaVersion") != 3',
            'lineage.get("stagingReportSchemaVersion") != 3',
            'if staging_authorization != envelope_authorization',
            'if lineage_authorization != envelope_authorization',
            '"stagingAuthorization": dict(envelope_authorization)',
            '"stagingAuthorization": dict(chain["stagingAuthorization"])',
        ):
            self.assertIn(marker, text)
        validate_pos = text.index("chain = validate_p1_chain(")
        child_pos = text.index("prepare_candidate_device.main(child_args)")
        self.assertLess(validate_pos, child_pos)


if __name__ == "__main__":
    unittest.main()
