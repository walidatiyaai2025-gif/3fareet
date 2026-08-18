import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
INTAKE = REPO_ROOT / "tools/android/import_hero_refinement_candidate_windows.ps1"


class HeroRefinementImportHandoffContractTests(unittest.TestCase):
    def test_companion_handoff_is_verified_before_copy_or_unity_staging(self):
        source = INTAKE.read_text(encoding="utf-8")
        for required in (
            '[string]$CompanionGlb = ""',
            '[string]$CompanionBlend = ""',
            '[string]$HandoffReceipt = ""',
            '[string]$RefinementManifest = ""',
            '$companionGlbProvided -xor $companionBlendProvided',
            'verify_hero_refinement_handoff_windows.ps1',
            '& $handoffVerifier @handoffParams',
            'AFAREET_HERO_REFINEMENT_HANDOFF_PREFLIGHT_OK productionGate=false verified=false',
        ):
            self.assertIn(required, source)

        verify_index = source.index('& $handoffVerifier @handoffParams')
        copy_index = source.index('Copy-Item -LiteralPath $SourceFbx -Destination $destination')
        unity_index = source.index('HeroCarRefinementCandidateStager.StageCurrentCandidate')
        self.assertLess(verify_index, copy_index)
        self.assertLess(verify_index, unity_index)

    def test_fbx_only_path_remains_backward_compatible_and_hash_pinned(self):
        source = INTAKE.read_text(encoding="utf-8")
        self.assertIn('if ($companionGlbProvided -and $companionBlendProvided)', source)
        self.assertIn(
            '$ExpectedSha256 = "97b02c87118c451d068c881fc551787d6e468ec8002cce7802db62258cc4cda2"',
            source,
        )
        self.assertIn('$ExpectedSize = 1475244', source)
        self.assertIn('Get-FileHash -LiteralPath $SourceFbx -Algorithm SHA256', source)
        self.assertIn('AFAREET_HERO_REFINEMENT_INTAKE_OK', source)

    def test_metadata_overrides_cannot_bypass_missing_companions(self):
        source = INTAKE.read_text(encoding="utf-8")
        self.assertIn(
            '($receiptOverrideProvided -or $manifestOverrideProvided) -and -not ($companionGlbProvided -and $companionBlendProvided)',
            source,
        )
        self.assertIn('Handoff receipt/manifest overrides require both -CompanionGlb and -CompanionBlend.', source)

    def test_intake_never_claims_production_or_verification(self):
        source = INTAKE.read_text(encoding="utf-8")
        self.assertIn('productionGate=false verified=false', source)
        self.assertNotIn('productionGate=true', source)
        self.assertNotIn('verified=true', source)


if __name__ == "__main__":
    unittest.main()
