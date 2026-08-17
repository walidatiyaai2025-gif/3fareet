import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PREFLIGHT_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionSourcePreflight.cs"
STAGER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionPrefabStager.cs"
RESOLVER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalImportedLodResolver.cs"
REVIEW_STAGER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalAuthoredReviewPrefabStager.cs"
STACK_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"
RUNTIME_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs"
MARKER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/RivalAuthoredReviewCandidateMarker.cs"


class RivalSourcePreflightContractTests(unittest.TestCase):
    def test_preflight_and_stager_share_exact_three_tracked_sources(self):
        preflight = PREFLIGHT_PATH.read_text(encoding="utf-8")
        stager = STAGER_PATH.read_text(encoding="utf-8")
        sources = (
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj",
        )
        for source in sources:
            self.assertIn(source, preflight)
            self.assertIn(source, stager)
        self.assertIn("SourcePaths.Length != RivalProductionPolicy.VariantCount", preflight)
        self.assertIn("distinctSources=3", preflight)

    def test_preflight_is_read_only_and_checks_production_surface_contract(self):
        source = PREFLIGHT_PATH.read_text(encoding="utf-8")
        required = (
            "AssetImporter.GetAtPath(sourcePath)",
            "AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)",
            "AssetDatabase.GetAssetPath(mesh)",
            "VertexAttribute.TexCoord0",
            "VertexAttribute.Normal",
            "material.GetTexturePropertyNames()",
            "material.GetTexture(propertyName)",
            "RivalProductionPolicy.MeetsProductionFloor",
            "RivalImportedLodResolver.ParseSourceOrThrow(sourcePath)",
            "RivalImportedLodResolver.ResolveImportedMeshesOrThrow(sourcePath, sourceModel, signature)",
            "RivalImportedLodResolver.ResolveImportedMaterialsOrThrow(sourcePath, sourceModel, signature)",
            "AFAREET_UART004_SOURCE_PREFLIGHT_OK",
            "AFAREET_UART004_SOURCE_PREFLIGHT_ALL_OK",
        )
        for token in required:
            self.assertIn(token, source)

        forbidden = (
            "PrefabUtility.SaveAsPrefabAsset",
            "AssetDatabase.CreateAsset",
            "GameObject.CreatePrimitive",
            "new Mesh",
            "RivalProductionSourceBinder.BindSource",
        )
        for token in forbidden:
            self.assertNotIn(token, source)

    def test_flattened_unity_obj_resolution_uses_mesh_subassets_and_exact_source_signature(self):
        resolver = RESOLVER_PATH.read_text(encoding="utf-8")
        required = (
            "using UnityEditor;",
            "File.ReadLines(sourcePath)",
            'line.StartsWith("o ", StringComparison.Ordinal)',
            'line.StartsWith("usemtl ", StringComparison.Ordinal)',
            'line.StartsWith("f ", StringComparison.Ordinal)',
            "triangles[currentLod] += vertices - 2",
            "MaterialNames",
            "AssetDatabase.LoadAllAssetsAtPath(sourcePath)",
            "ResolveImportedMeshesOrThrow",
            "ResolveImportedMaterialsOrThrow",
            "ResolveLodFromName(current.name)",
            "ResolveLodFromName(mesh.name)",
            "var meshTriangles = TriangleCount(mesh)",
            "meshTriangles != signature.Triangles[lod]",
            "source triangle signatures are ambiguous",
            "DescribeImportedTopology",
        )
        for token in required:
            self.assertIn(token, resolver)

        self.assertNotIn("MeetsProductionFloor", resolver)
        self.assertNotIn("MinimumTriangles[lod] <=", resolver)
        self.assertNotIn("MaximumTriangles[lod] >=", resolver)
        self.assertNotIn("new Mesh(", resolver)
        self.assertNotIn("GameObject.CreatePrimitive", resolver)

    def test_preflight_and_production_stager_keep_the_same_shared_subasset_resolver(self):
        preflight = PREFLIGHT_PATH.read_text(encoding="utf-8")
        stager = STAGER_PATH.read_text(encoding="utf-8")
        for source in (preflight, stager):
            self.assertIn("RivalImportedLodResolver.ParseSourceOrThrow(sourcePath)", source)
            self.assertIn("RivalImportedLodResolver.ResolveImportedMeshesOrThrow(sourcePath, sourceModel, signature)", source)
            self.assertIn("RivalImportedLodResolver.ResolveImportedMaterialsOrThrow(sourcePath, sourceModel, signature)", source)
            self.assertIn("resolver=imported-mesh-subassets+exact-source-signature", source)

    def test_authored_review_packaging_is_source_exact_local_only_and_non_production(self):
        stager = REVIEW_STAGER_PATH.read_text(encoding="utf-8")
        marker = MARKER_PATH.read_text(encoding="utf-8")
        runtime = RUNTIME_PATH.read_text(encoding="utf-8")
        ignore = (REPO_ROOT / ".gitignore").read_text(encoding="utf-8")

        for required in (
            "RivalImportedLodResolver.ParseSourceOrThrow(sourcePath)",
            "WriteExactLodPackageOrThrow",
            'line.StartsWith("v ", StringComparison.Ordinal)',
            'line.StartsWith("vt ", StringComparison.Ordinal)',
            'line.StartsWith("vn ", StringComparison.Ordinal)',
            'line.StartsWith("f ", StringComparison.Ordinal)',
            "review package triangle identity mismatch",
            "ReviewPackaging",
            "PF_Rival_{variant + 1:00}_AuthoredReview",
            "RivalAuthoredReviewCandidateMarker",
            "geometryChanged=false",
            "productionGate=false",
            "p1Gate=false",
        ):
            self.assertIn(required, stager)

        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh(",
            "RivalProductionSourceBinder.BindSource",
            "RivalProductionAssetMetadata>() ??",
        ):
            self.assertNotIn(forbidden, stager)

        self.assertIn('ExpectedClassification = "AUTHORED_REVIEW_CANDIDATE"', marker)
        self.assertIn("CanSatisfyProductionGate => false", marker)
        self.assertIn("AFAREET_UART004_AUTHORED_REVIEW_RIVAL_ACTIVE", runtime)
        self.assertIn("TryValidateAuthoredReview", runtime)
        self.assertIn("production=false p1Gate=false", runtime)
        self.assertIn("Assets/Afareet/ArtSource/Vehicles/Rivals/ReviewPackaging/", ignore)
        self.assertIn("Assets/Afareet/Resources/Art/Vehicles/Rivals/Review/", ignore)

    def test_full_visual_stack_uses_review_preflight_before_first_mutation_and_never_promotes_uart004(self):
        source = STACK_PATH.read_text(encoding="utf-8")
        hero = source.index("HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow")
        rival = source.index("RivalAuthoredReviewPrefabStager.ValidateCurrentSourcesOrThrow")
        first_stage = source.index("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow")
        rival_stage = source.index("RivalAuthoredReviewPrefabStager.StageAll")

        self.assertLess(hero, rival)
        self.assertLess(rival, first_stage)
        self.assertLess(first_stage, rival_stage)
        self.assertIn('"UART-004 rival tracked OBJ review sources"', source)
        self.assertIn("uart004=authored-review-candidates", source)
        self.assertIn("productionGate=false", source)
        self.assertNotIn("RivalProductionPrefabStager.StageAndBindAll", source)
        self.assertNotIn("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow", source)


if __name__ == "__main__":
    unittest.main()
