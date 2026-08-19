import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uart004ProductionRivalContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_player_runtime_uses_authored_rival_resources(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs")
        self.assertIn("RivalProductionPolicy.ResourcePath", text)
        self.assertIn("ValidateProductionPrefab", text)
        self.assertIn("AFAREET_UART004_AUTHORED_RIVAL_ACTIVE", text)
        self.assertIn("fingerprint=", text)
        self.assertIn("physicsRootPreserved=true", text)
        self.assertIn("primitive-fallback-disabled", text)

    def test_primitive_variant_creation_is_editor_only(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs")
        self.assertIn("#if UNITY_EDITOR", text)
        self.assertIn("ApplyEditorBlockoutVariant", text)
        self.assertIn("GameObject.CreatePrimitive(PrimitiveType.Cube)", text)
        self.assertIn("AFAREET_UART004_EDITOR_BLOCKOUT_RIVAL_ACTIVE", text)

    def test_android_gate_requires_preexisting_external_authored_prefabs_and_verifiable_sources(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionBuildGate.cs")
        self.assertNotIn("RivalProductionAssetBuilder.BuildOrThrow", text)
        for required in (
            "RivalProductionPolicy.VariantCount",
            "RivalProductionPolicy.AssetPath",
            "ValidateProductionPrefab",
            "missing-external-authored-prefab",
            "IsExactProductionSourceForVariant",
            "AssetDatabase.LoadMainAssetAtPath",
            "AssetImporter.GetAtPath",
            "AssetDatabase.AssetPathToGUID",
            "AssetDatabase.GetAssetDependencyHash",
            "RivalProductionPolicy.MeshFor(renderer)",
            "AssetDatabase.GetAssetPath(mesh)",
            "source-guid-mismatch",
            "source-dependency-hash-mismatch",
            "mesh-not-backed-by-source",
            "AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED",
            "AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK",
            "exactVariantSources=true",
            "guidHashBound=true",
            "meshSourceBound=true",
        ):
            self.assertIn(required, text)

    def test_android_gate_requires_three_distinct_authored_model_sources(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionBuildGate.cs")
        self.assertIn("HashSet<string>", text)
        self.assertIn("usedSourceGuids", text)
        self.assertIn("duplicate-authored-source-guid", text)
        self.assertIn("expected=three-distinct-rival-model-sources", text)
        self.assertIn("distinctSources=3", text)

    def test_source_binder_records_real_unity_provenance_and_refuses_cross_variant_reuse(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionSourceBinder.cs")
        for required in (
            "Afareet/Bind UART-004/",
            "Rival 1 Source",
            "Rival 2 Source",
            "Rival 3 Source",
            "internal static void BindSource",
            "RivalProductionPolicy.IsExactProductionSourceForVariant",
            "RivalProductionPolicy.StagingSourcePath",
            "EnsureSourceIsUniqueAcrossOtherVariants",
            "AssetDatabase.AssetPathToGUID",
            "AssetDatabase.GetAssetDependencyHash",
            "AssetDatabase.GetAssetPath(mesh)",
            "mesh is not backed by selected source",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "metadata.Configure",
            "ValidateProductionPrefab",
            "AFAREET_UART004_SOURCE_BIND_OK",
            "exactVariantSource=true",
        ):
            self.assertIn(required, text)
        self.assertIn("cannot reuse rival", text)
        self.assertNotIn("GameObject.CreatePrimitive", text)

    def test_prefab_stager_uses_only_isolated_production_sources_and_preflights_all_before_mutation(self):
        path = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionPrefabStager.cs"
        self.assertTrue(path.is_file())
        self.assertTrue(path.with_suffix(path.suffix + ".meta").is_file())
        text = path.read_text(encoding="utf-8")

        for required in (
            "RivalProductionPolicy.StagingSourcePath(0)",
            "RivalProductionPolicy.StagingSourcePath(1)",
            "RivalProductionPolicy.StagingSourcePath(2)",
            "internal static void ValidateAllSourcesBeforeMutation()",
            "ValidateSourceAvailable(variant)",
            "AFAREET_UART004_SOURCE_PREFLIGHT_OK",
            "mutationStarted=false",
            "Stage + Bind All Rival Prefabs",
            "AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)",
            "PrefabUtility.InstantiatePrefab(sourceModel)",
            "GetComponentsInChildren<Renderer>(true)",
            "ResolveLod",
            'EndsWith($"_LOD{lod}"',
            "RivalProductionPolicy.MeshFor(renderer)",
            "AssetDatabase.GetAssetPath(mesh)",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "new LOD(",
            "group.SetLODs(lods)",
            "PrefabUtility.SaveAsPrefabAsset",
            "RivalProductionSourceBinder.BindSource",
            "RivalProductionPolicy.ValidateProductionPrefab",
            "AFAREET_UART004_PREFAB_STAGE_OK",
            "geometryGenerated=false",
            "primitiveCreated=false",
        ):
            self.assertIn(required, text)

        self.assertLess(text.index("ValidateAllSourcesBeforeMutation();"), text.index("for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)\n                StageAndBind(variant);"))

        for review_source in (
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj",
        ):
            self.assertNotIn(review_source, text)

        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh(",
            "new Mesh {",
            "MeshFilter.mesh =",
            "MeshFilter.sharedMesh = new",
        ):
            self.assertNotIn(forbidden, text)

    def test_rival_quality_contract_requires_isolated_production_root_and_external_model_source(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs")
        for required in (
            'ProductionSourceRoot = "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/"',
            'ProductionSourceRoot + "Rival_01_WedgeCoupe_Production.obj"',
            'ProductionSourceRoot + "Rival_02_FastbackMuscle_Production.obj"',
            'ProductionSourceRoot + "Rival_03_CompactPrototype_Production.obj"',
            "StagingSourcePath",
            "IsExactProductionSourceForVariant",
            "authoredExternalSource",
            "uv0Authored",
            "normalsAuthored",
            "textureMappedMaterials",
            "assetVersion",
            "sourceFingerprint",
            "sourceGuid",
            "sourceDependencyHash",
            "IsSupportedAuthoredModelSource",
            "StartsWith(ProductionSourceRoot",
            '"/Generated/"',
            '"/Preview/"',
            '"/Refinement/"',
            '"/RefinementCandidates/"',
            '"/Blockout/"',
            '"/Review/"',
            '"/ReviewPackaging/"',
            '".fbx"',
            '".obj"',
            '".blend"',
            '".glb"',
            '".gltf"',
        ):
            self.assertIn(required, text)

    def test_tracked_design_pack_is_brief_not_production_provenance(self):
        path = REPO_ROOT / "docs/assets/01_vehicles/rival_cars_production/RIVAL_DESIGN_PROFILES.json"
        pack = json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual(pack["reviewState"], "BLOCKED_PENDING_UNITY_APK_VISUAL_PROOF")
        self.assertEqual(len(pack["variants"]), 3)
        self.assertEqual(len({v["id"] for v in pack["variants"]}), 3)
        self.assertEqual(len({v["displayName"] for v in pack["variants"]}), 3)
        self.assertEqual(len(pack["lodTopology"]), 3)
        self.assertTrue(pack["productionIntent"]["preserveExistingPhysics"])
        self.assertFalse(pack["productionIntent"]["playerPrimitiveFallbackAllowed"])

        readme = self._read("docs/assets/01_vehicles/rival_cars_production/README.md")
        self.assertIn("design/LOD-budget brief", readme)
        self.assertIn("not production provenance", readme)
        self.assertIn("externally authored model", readme)
        self.assertIn("does **not** synthesize production art", readme)

    def test_code_generated_production_builder_is_not_present(self):
        self.assertFalse(
            (REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionAssetBuilder.cs").exists(),
            "UART-004 must not ship a code-generated mesh builder as production provenance",
        )

    def test_external_rival_resources_are_trackable(self):
        ignore = self._read(".gitignore")
        for forbidden_ignore in (
            "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/\n",
            "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals.meta\n",
            "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/\n",
            "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/\n",
        ):
            self.assertNotIn(
                forbidden_ignore,
                ignore,
                "Externally authored UART-004 production assets must be commit-able and must not be hidden by .gitignore",
            )


if __name__ == "__main__":
    unittest.main()
