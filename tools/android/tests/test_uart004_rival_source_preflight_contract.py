import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PREFLIGHT_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionSourcePreflight.cs"
STAGER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionPrefabStager.cs"
RESOLVER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalImportedLodResolver.cs"
STACK_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"


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

        # The final fallback must be an exact source-derived identity match, not a broad
        # production quality range that could assign one imported mesh to several LODs.
        self.assertNotIn("MeetsProductionFloor", resolver)
        self.assertNotIn("MinimumTriangles[lod] <=", resolver)
        self.assertNotIn("MaximumTriangles[lod] >=", resolver)
        self.assertNotIn("new Mesh(", resolver)
        self.assertNotIn("GameObject.CreatePrimitive", resolver)

    def test_preflight_and_stager_use_the_same_shared_subasset_resolver(self):
        preflight = PREFLIGHT_PATH.read_text(encoding="utf-8")
        stager = STAGER_PATH.read_text(encoding="utf-8")
        for source in (preflight, stager):
            self.assertIn("RivalImportedLodResolver.ParseSourceOrThrow(sourcePath)", source)
            self.assertIn("RivalImportedLodResolver.ResolveImportedMeshesOrThrow(sourcePath, sourceModel, signature)", source)
            self.assertIn("RivalImportedLodResolver.ResolveImportedMaterialsOrThrow(sourcePath, sourceModel, signature)", source)
            self.assertIn("resolver=imported-mesh-subassets+exact-source-signature", source)

    def test_full_visual_stack_runs_all_preflights_before_first_mutating_stage(self):
        source = STACK_PATH.read_text(encoding="utf-8")
        hero = source.index("HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow")
        rival = source.index("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow")
        first_stage = source.index("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow")
        rival_stage = source.index("RivalProductionPrefabStager.StageAndBindAll")

        self.assertLess(hero, rival)
        self.assertLess(rival, first_stage)
        self.assertLess(first_stage, rival_stage)
        self.assertIn('"UART-004 rival authored model sources"', source)


if __name__ == "__main__":
    unittest.main()
