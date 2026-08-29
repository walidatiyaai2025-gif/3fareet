import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Progression/CareerSaveCodec.cs"
TESTS = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerSaveCodecTests.cs"
COMPILE = REPO_ROOT / "tools/android/contracts/CareerFoundationCompile.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/CareerSaveCodecContractRunner.cs"
RUNNER_PROJECT = REPO_ROOT / "tools/android/contracts/CareerSaveCodecContractRunner.csproj"


class CareerSaveCodecContractTests(unittest.TestCase):
    def test_codec_is_storage_neutral_and_version_bounded(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("public sealed class CareerSaveCodec", source)
        self.assertIn("public const int MaxStoredStars = 9999;", source)
        self.assertIn("public string Encode(CareerProgress progress)", source)
        self.assertIn("public CareerProgress Decode(string source)", source)
        for forbidden in ("UnityEngine", "PlayerPrefs", "System.IO", "File.", "Directory."):
            self.assertNotIn(forbidden, source)

    def test_canonical_current_and_legacy_fields_are_explicit(self):
        source = SOURCE.read_text(encoding="utf-8")
        for token in (
            '\\"version\\"',
            '\\"stars\\"',
            '\\"completedNodeIds\\"',
            '\\"claimedRewardIds\\"',
            '"totalStars"',
            '"completed"',
        ):
            self.assertIn(token, source)
        self.assertIn("version.Value == 0", source)
        self.assertIn("version.Value == CareerProgress.CurrentVersion", source)
        self.assertIn("Unsupported Career save version", source)

    def test_parser_fails_closed_on_ambiguous_or_malformed_json(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("Duplicate JSON property", source)
        self.assertIn("Unexpected trailing content", source)
        self.assertIn("Career save root must be an object", source)
        self.assertIn("JSON nesting is too deep", source)
        self.assertIn("Career save field '{key}' must be an integer", source)
        self.assertIn("Career save field '{key}' must be an array", source)

    def test_nunit_regressions_cover_roundtrip_migration_and_failures(self):
        tests = TESTS.read_text(encoding="utf-8")
        for method in (
            "EncodeDecode_RoundTripsDeterministicallyAndEscapesIds",
            "Decode_MigratesLegacyV0AndFiltersInvalidListMembers",
            "Decode_CurrentV1ClampsNegativeStarsAndKeepsClaims",
            "Encode_RejectsProgressAbovePersistenceBound",
            "Decode_RejectsMalformedRootsTypesDuplicateKeysAndUnsupportedVersions",
            "Decode_AcceptsExplicitLegacyVersionZero",
        ):
            self.assertIn(method, tests)

    def test_shared_netstandard_compile_gate_includes_codec(self):
        project = COMPILE.read_text(encoding="utf-8")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        self.assertIn("CareerProgression.cs", project)
        self.assertIn("CareerSaveCodec.cs", project)

    def test_executable_runner_is_isolated_and_compiles_authoritative_sources(self):
        runner = RUNNER.read_text(encoding="utf-8")
        project = RUNNER_PROJECT.read_text(encoding="utf-8")
        self.assertIn("canonical encode/decode must be deterministic", runner)
        self.assertIn("legacy stars must clamp to persisted bound", runner)
        self.assertIn("duplicate JSON keys must fail closed", runner)
        self.assertIn("<BaseIntermediateOutputPath>obj/CareerSaveCodecContractRunner/</BaseIntermediateOutputPath>", project)
        self.assertIn("CareerDefinition.cs", project)
        self.assertIn("CareerProgression.cs", project)
        self.assertIn("CareerSaveCodec.cs", project)


if __name__ == "__main__":
    unittest.main()
