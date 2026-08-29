import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uart004ExactSourceBindingContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_policy_maps_each_variant_to_one_exact_exchange_source(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs")

        for required in (
            "public static bool IsExactProductionSourceForVariant",
            "StagingSourcePaths[variantIndex]",
            "string.Equals(normalized, StagingSourcePaths[variantIndex], StringComparison.Ordinal)",
            'ProductionSourceRoot + "Rival_01_WedgeCoupe_Production.obj"',
            'ProductionSourceRoot + "Rival_02_FastbackMuscle_Production.obj"',
            'ProductionSourceRoot + "Rival_03_CompactPrototype_Production.obj"',
            "RivalProductionPolicy.IsExactProductionSourceForVariant(variantIndex, sourceAssetId)",
            "IsExactProductionSourceForVariant(variantIndex, metadata.SourceAssetId)",
            "unexpected-authored-model-source",
        ):
            self.assertIn(required, text)

    def test_direct_binder_requires_exact_source_before_import_or_metadata_mutation(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionSourceBinder.cs")

        self.assertIn("RivalProductionPolicy.IsExactProductionSourceForVariant(variant, sourcePath)", text)
        self.assertIn("var expectedSourcePath = RivalProductionPolicy.StagingSourcePath(variant);", text)
        self.assertIn("source must match its deterministic production exchange path", text)
        self.assertIn("exactVariantSource=true", text)
        self.assertLess(
            text.index("IsExactProductionSourceForVariant(variant, sourcePath)"),
            text.index("AssetImporter.GetAtPath(sourcePath)"),
        )
        self.assertNotIn("return RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath)", text)

    def test_android_gate_rejects_wrong_variant_source_before_provenance_hash_checks(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionBuildGate.cs")

        self.assertIn("RivalProductionPolicy.IsExactProductionSourceForVariant(variant, sourcePath)", text)
        self.assertIn("var expectedSourcePath = RivalProductionPolicy.StagingSourcePath(variant);", text)
        self.assertIn("reason=unexpected-authored-model-source expected=", text)
        self.assertIn("exactVariantSources=true", text)
        self.assertLess(
            text.index("IsExactProductionSourceForVariant(variant, sourcePath)"),
            text.index("AssetDatabase.AssetPathToGUID(sourcePath)"),
        )
        self.assertNotIn("RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath)", text)

    def test_generic_production_root_support_cannot_satisfy_metadata_on_its_own(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs")
        declaration = text.split("public bool DeclaresProductionAuthoring =>", 1)[1].split("public void Configure", 1)[0]
        self.assertIn("IsExactProductionSourceForVariant", declaration)
        self.assertNotIn("IsSupportedAuthoredModelSource(sourceAssetId)", declaration)


if __name__ == "__main__":
    unittest.main()
