import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1VehicleCatalogContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_catalog_is_versioned_and_transport_ids_are_fail_closed(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleCatalog.cs")

        self.assertIn("public const int CurrentSchemaVersion = 1;", source)
        self.assertIn("catalog.SchemaVersion != VehicleCatalog.CurrentSchemaVersion", source)
        self.assertIn("new HashSet<string>(StringComparer.Ordinal)", source)
        self.assertIn("Duplicate vehicle definition id", source)
        self.assertIn("IsTransportSafeId", source)
        self.assertIn("MaxTransportIdLength = 64", source)
        self.assertIn("IsLowerAlphaNumeric(id[0])", source)
        self.assertIn("value == '-' || value == '_' || value == '.'", source)
        self.assertNotIn("using UnityEngine;", source)

    def test_catalog_normalized_stats_and_unlock_requirements_are_validated(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleCatalog.cs")

        for required in (
            "ValidateNormalizedStat(definition.Id, nameof(definition.TopSpeed), definition.TopSpeed)",
            "ValidateNormalizedStat(definition.Id, nameof(definition.Acceleration), definition.Acceleration)",
            "ValidateNormalizedStat(definition.Id, nameof(definition.Handling), definition.Handling)",
            "ValidateNormalizedStat(definition.Id, nameof(definition.Drift), definition.Drift)",
            "float.IsNaN(value)",
            "float.IsInfinity(value)",
            "value < 0f || value > 1f",
            "VehicleUnlockKind.Always",
            "VehicleUnlockKind.PlayerLevel",
            "VehicleUnlockKind.CareerStars",
            "requirement.Threshold < 0",
        ):
            self.assertIn(required, source)

    def test_unlock_filter_is_order_preserving_and_threshold_based(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleCatalog.cs")

        self.assertIn("foreach (var definition in catalog.Definitions)", source)
        self.assertIn("if (IsUnlocked(definition.UnlockRequirement, progress))", source)
        self.assertIn("unlocked.Add(definition);", source)
        self.assertIn("progress.PlayerLevel >= requirement.Threshold", source)
        self.assertIn("progress.CareerStars >= requirement.Threshold", source)

    def test_editmode_regression_suite_covers_acceptance_boundaries(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/VehicleCatalogPolicyTests.cs")
        asmdef = self._read("unity_game/Assets/Afareet/Tests/EditMode/Afareet.EditModeTests.asmdef")

        for required in (
            "ValidateOrThrow_AcceptsStableUniqueCatalog",
            "ValidateOrThrow_RejectsUnsupportedSchema",
            "ValidateOrThrow_RejectsDuplicateIds",
            "IsTransportSafeId_RejectsNonCanonicalIds",
            "ValidateOrThrow_RejectsOutOfRangeNormalizedStats",
            "ValidateOrThrow_RejectsNonFiniteNormalizedStats",
            "FilterUnlocked_PreservesCatalogOrderAndAppliesRequirements",
            "IsUnlocked_UsesInclusiveThresholds",
        ):
            self.assertIn(required, tests)

        self.assertIn('"Afareet.Vehicle"', asmdef)

    def test_unity_metadata_is_tracked_for_new_sources(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleCatalog.cs.meta")
        test_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/VehicleCatalogPolicyTests.cs.meta")

        self.assertIn("fileFormatVersion: 2", source_meta)
        self.assertIn("guid:", source_meta)
        self.assertIn("fileFormatVersion: 2", test_meta)
        self.assertIn("guid:", test_meta)


if __name__ == "__main__":
    unittest.main()
